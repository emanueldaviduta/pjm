using MailKit;
using MailKit.Net.Imap;
using MailKit.Security;
using System.Text;
using MimeKit;
using MimeKit.IO;
using MimeKit.IO.Filters;
using ProjectManager.Application.Common;
using ProjectManager.Application.Interfaces;
using ProjectManager.Domain.Entities;

namespace ProjectManager.Infrastructure.Services;

/// <summary>
/// IMAP access with MailKit. The folder is always opened read-only, so messages are never flagged, moved or deleted.
/// Only the body text and the attachments within <see cref="MailLimits"/> are downloaded.
/// </summary>
public class MailKitMailboxClient : IMailboxClient
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    /// <summary>Messages handled per poll; the rest follow on the next poll, since the cursor advances per message.</summary>
    private const int MaxMessagesPerFetch = 50;

    public async Task<MailboxTestResult> TestAsync(MailboxConnection connection, CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(Timeout);
        try
        {
            using var client = await ConnectAsync(connection, timeout.Token);
            var folder = await OpenFolderAsync(client, connection.Folder, timeout.Token);
            var count = folder.Count;
            await client.DisconnectAsync(true, timeout.Token);
            return new MailboxTestResult(true, $"Connected. The folder \"{folder.FullName}\" holds {count} messages.");
        }
        catch (AuthenticationException)
        {
            return new MailboxTestResult(false, "Authentication failed: the server rejected the username or password.");
        }
        catch (FolderNotFoundException)
        {
            return new MailboxTestResult(false, $"Connected, but the folder \"{connection.Folder}\" does not exist.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new MailboxTestResult(false, "The server did not answer within 30 seconds.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new MailboxTestResult(false, $"Could not connect: {ex.Message}");
        }
    }

    public async Task<MailboxFetchResult> FetchNewAsync(MailboxConnection connection, long? uidValidity, long? lastUid,
        CancellationToken cancellationToken = default)
    {
        using var client = await ConnectAsync(connection, cancellationToken);
        var folder = await OpenFolderAsync(client, connection.Folder, cancellationToken);
        long validity = folder.UidValidity;
        var highest = await HighestUidAsync(folder, cancellationToken);

        if (uidValidity != validity || lastUid == null || highest <= lastUid)
        {
            await client.DisconnectAsync(true, cancellationToken);
            return new MailboxFetchResult(validity, highest, []);
        }

        var range = new UniqueIdRange(new UniqueId(folder.UidValidity, (uint)lastUid.Value + 1), UniqueId.MaxValue);
        var summaries = await folder.FetchAsync(range,
            MessageSummaryItems.UniqueId | MessageSummaryItems.Envelope | MessageSummaryItems.BodyStructure | MessageSummaryItems.InternalDate,
            cancellationToken);

        var messages = new List<MailboxMessage>();
        // "n:*" always includes the newest message, even when its UID is below n, so filter again.
        foreach (var summary in summaries.Where(s => s.UniqueId.Id > lastUid).OrderBy(s => s.UniqueId.Id).Take(MaxMessagesPerFetch))
        {
            messages.Add(await ReadMessageAsync(folder, summary, cancellationToken));
        }

        await client.DisconnectAsync(true, cancellationToken);
        return new MailboxFetchResult(validity, highest, messages);
    }

    private static async Task<ImapClient> ConnectAsync(MailboxConnection connection, CancellationToken cancellationToken)
    {
        var client = new ImapClient { Timeout = (int)Timeout.TotalMilliseconds };
        try
        {
            await client.ConnectAsync(connection.Host, connection.Port, ToSocketOptions(connection.Security), cancellationToken);
            await client.AuthenticateAsync(connection.Username, connection.Password, cancellationToken);
            return client;
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    private static async Task<IMailFolder> OpenFolderAsync(ImapClient client, string name, CancellationToken cancellationToken)
    {
        var folder = string.Equals(name, "INBOX", StringComparison.OrdinalIgnoreCase)
            ? client.Inbox
            : await client.GetFolderAsync(name, cancellationToken);
        await folder.OpenAsync(FolderAccess.ReadOnly, cancellationToken);
        return folder;
    }

    private static async Task<long> HighestUidAsync(IMailFolder folder, CancellationToken cancellationToken)
    {
        if (folder.Count == 0)
        {
            return folder.UidNext is { } next ? next.Id - 1 : 0;
        }

        var last = await folder.FetchAsync(folder.Count - 1, folder.Count - 1, MessageSummaryItems.UniqueId, cancellationToken);
        return last.Count == 0 ? 0 : last[0].UniqueId.Id;
    }

    private static async Task<MailboxMessage> ReadMessageAsync(IMailFolder folder, IMessageSummary summary, CancellationToken cancellationToken)
    {
        var envelope = summary.Envelope;
        var from = envelope?.From.Mailboxes.FirstOrDefault()?.Address ?? "";
        var receivedAt = (envelope?.Date ?? summary.InternalDate)?.UtcDateTime;

        var body = "";
        if (summary.TextBody is { } textPart)
        {
            body = await DownloadTextAsync(folder, summary.UniqueId, textPart, cancellationToken);
        }
        else if (summary.HtmlBody is { } htmlPart)
        {
            body = HtmlText.ToPlainText(await DownloadTextAsync(folder, summary.UniqueId, htmlPart, cancellationToken));
        }

        var attachments = new List<MailboxAttachment>();
        foreach (var part in AttachmentParts(summary))
        {
            var fileName = part.FileName!;
            var contentType = part.ContentType.MimeType.ToLowerInvariant();
            var size = DecodedSize(part);
            if (attachments.Count(a => a.Content != null) >= MailLimits.MaxAttachmentsPerEmail)
            {
                attachments.Add(new(fileName, contentType, size, null, $"more than {MailLimits.MaxAttachmentsPerEmail} files"));
                continue;
            }
            if (size > MailLimits.MaxAttachmentBytes)
            {
                attachments.Add(new(fileName, contentType, size, null, "larger than 10 MB"));
                continue;
            }

            var content = await DownloadAsync(folder, summary.UniqueId, part, cancellationToken);
            attachments.Add(content.LongLength > MailLimits.MaxAttachmentBytes
                ? new(fileName, contentType, content.LongLength, null, "larger than 10 MB")
                : new(fileName, contentType, content.LongLength, content, null));
        }

        return new MailboxMessage(summary.UniqueId.Id, envelope?.MessageId, from, envelope?.Subject ?? "", receivedAt, body, attachments);
    }

    /// <summary>
    /// Fetches one part's content (BODY[n]) and decodes its transfer encoding from the BODYSTRUCTURE. This avoids
    /// fetching the part's MIME headers separately, which some servers answer incorrectly.
    /// </summary>
    private static async Task<byte[]> DownloadAsync(IMailFolder folder, UniqueId uid, BodyPartBasic part, CancellationToken cancellationToken)
    {
        using var raw = await folder.GetStreamAsync(uid, part.PartSpecifier, cancellationToken);
        using var decoded = new MemoryStream();
        using (var filtered = new FilteredStream(decoded))
        {
            filtered.Add(DecoderFilter.Create(ParseEncoding(part.ContentTransferEncoding)));
            await raw.CopyToAsync(filtered, cancellationToken);
            await filtered.FlushAsync(cancellationToken);
        }
        return decoded.ToArray();
    }

    private static async Task<string> DownloadTextAsync(IMailFolder folder, UniqueId uid, BodyPartBasic part, CancellationToken cancellationToken)
    {
        var bytes = await DownloadAsync(folder, uid, part, cancellationToken);
        Encoding encoding;
        try
        {
            encoding = part.ContentType.Charset is { Length: > 0 } charset ? Encoding.GetEncoding(charset) : Encoding.UTF8;
        }
        catch (ArgumentException)
        {
            encoding = Encoding.UTF8;
        }
        return encoding.GetString(bytes);
    }

    private static ContentEncoding ParseEncoding(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "base64" => ContentEncoding.Base64,
        "quoted-printable" => ContentEncoding.QuotedPrintable,
        "x-uuencode" or "uuencode" => ContentEncoding.UUEncode,
        "8bit" => ContentEncoding.EightBit,
        "binary" => ContentEncoding.Binary,
        _ => ContentEncoding.SevenBit
    };

    /// <summary>Parts marked as attachments, plus inline parts that carry a file name (such as named inline images).</summary>
    private static IEnumerable<BodyPartBasic> AttachmentParts(IMessageSummary summary) =>
        summary.BodyParts.Where(p =>
            p.FileName != null &&
            p != summary.TextBody && p != summary.HtmlBody &&
            (p.IsAttachment || string.Equals(p.ContentDisposition?.Disposition, ContentDisposition.Inline, StringComparison.OrdinalIgnoreCase)));

    // Octets is the encoded size; base64 grows content by a third, so estimate the decoded size before downloading.
    private static long DecodedSize(BodyPartBasic part) =>
        string.Equals(part.ContentTransferEncoding, "base64", StringComparison.OrdinalIgnoreCase)
            ? part.Octets * 3 / 4
            : (long)part.Octets;

    private static SecureSocketOptions ToSocketOptions(MailSecurity security) => security switch
    {
        MailSecurity.SslOnConnect => SecureSocketOptions.SslOnConnect,
        MailSecurity.StartTls => SecureSocketOptions.StartTls,
        MailSecurity.None => SecureSocketOptions.None,
        _ => SecureSocketOptions.Auto
    };
}
