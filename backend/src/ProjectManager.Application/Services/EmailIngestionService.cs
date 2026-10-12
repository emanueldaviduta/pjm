using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ProjectManager.Application.Common;
using ProjectManager.Application.Interfaces;
using ProjectManager.Domain.Entities;

namespace ProjectManager.Application.Services;

public class EmailIngestionService(
    IAppDbContext context,
    IMailboxClient mailbox,
    IFileStorage fileStorage,
    ISecretProtector protector) : IEmailIngestionService
{
    private const string AttachmentFolder = "pjm/tasks";

    public async Task<MailPollOutcome> PollOnceAsync(CancellationToken cancellationToken = default)
    {
        var settings = await context.MailSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings is not { Enabled: true })
        {
            return new MailPollOutcome(false, settings?.PollIntervalSeconds ?? 60, 0, null);
        }

        // The cursor stays where it is, so mail that arrives meanwhile is imported once this is fixed.
        if (settings.ProjectId is not { } projectId ||
            !await context.Projects.AnyAsync(p => p.Id == projectId && !p.IsDeleted, cancellationToken))
        {
            return await Record(settings, 0, "The target project is missing or has been deleted.", cancellationToken);
        }
        if (settings.ProtectedPassword == null)
        {
            return await Record(settings, 0, "No mailbox password is stored.", cancellationToken);
        }

        var connection = new MailboxConnection(settings.Host, settings.Port, settings.Security, settings.Username,
            protector.Unprotect(settings.ProtectedPassword), settings.Folder);
        var mailboxKey = MailboxKey(settings);
        var created = 0;
        try
        {
            var hasCursor = settings.MailboxKey == mailboxKey && settings.UidValidity != null && settings.LastUid != null;
            var result = await mailbox.FetchNewAsync(connection,
                hasCursor ? settings.UidValidity : null, hasCursor ? settings.LastUid : null, cancellationToken);

            // First poll after enabling, another mailbox, or UIDVALIDITY reset: only mail from here on is imported.
            if (!hasCursor || result.UidValidity != settings.UidValidity)
            {
                settings.MailboxKey = mailboxKey;
                settings.UidValidity = result.UidValidity;
                settings.LastUid = result.HighestUid;
                return await Record(settings, 0, null, cancellationToken);
            }

            foreach (var message in result.Messages.OrderBy(m => m.Uid))
            {
                if (await Process(settings, mailboxKey, projectId, message, cancellationToken))
                {
                    created++;
                }
            }
            return await Record(settings, created, null, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The message only; connection details and credentials are never stored.
            return await Record(settings, created, ex.Message, cancellationToken);
        }
    }

    /// <returns>True when a task was created. The task, its attachments, the InboundEmail row and the cursor are saved together.</returns>
    private async Task<bool> Process(MailSettings settings, string mailboxKey, int projectId, MailboxMessage message,
        CancellationToken cancellationToken)
    {
        var messageKey = string.IsNullOrWhiteSpace(message.MessageId)
            ? $"uid:{settings.UidValidity}:{message.Uid}"
            : message.MessageId.Trim();
        if (await context.InboundEmails.AnyAsync(e => e.MailboxKey == mailboxKey && e.MessageKey == messageKey, cancellationToken))
        {
            settings.LastUid = message.Uid;
            await context.SaveChangesAsync(cancellationToken);
            return false;
        }

        var inbound = new InboundEmail
        {
            MailboxKey = mailboxKey,
            MessageKey = messageKey,
            From = message.From,
            Subject = Truncate(message.Subject, 500),
            ReceivedAt = message.ReceivedAt,
            ProcessedAt = DateTime.UtcNow
        };
        settings.LastUid = message.Uid;

        TaskItem? task = null;
        if (settings.AllowedSenders.Any(s => string.Equals(s, message.From, StringComparison.OrdinalIgnoreCase)))
        {
            var (attachments, notAttached) = await StoreAttachments(message, cancellationToken);
            task = new TaskItem
            {
                ProjectId = projectId,
                Title = Title(message.Subject),
                Description = Description(message, notAttached),
                Status = Status.Created,
                Priority = Priority.Medium,
                DueDate = null,
                AssignedId = null,
                Attachments = attachments
            };
            inbound.Outcome = InboundEmailOutcome.Created;
            inbound.TaskItem = task;
            context.TaskItems.Add(task);
        }
        else
        {
            inbound.Outcome = InboundEmailOutcome.Skipped;
        }

        context.InboundEmails.Add(inbound);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return task != null;
        }
        catch (DbUpdateException)
        {
            // Another poller recorded the same message first (unique MailboxKey + MessageKey): drop ours, keep the cursor.
            context.InboundEmails.Remove(inbound);
            if (task != null)
            {
                context.TaskAttachments.RemoveRange(task.Attachments);
                context.TaskItems.Remove(task);
            }
            await context.SaveChangesAsync(cancellationToken);
            return false;
        }
    }

    private async Task<(List<TaskAttachment> Stored, List<string> NotAttached)> StoreAttachments(
        MailboxMessage message, CancellationToken cancellationToken)
    {
        var stored = new List<TaskAttachment>();
        var notAttached = new List<string>();
        foreach (var attachment in message.Attachments)
        {
            if (attachment.Content == null)
            {
                notAttached.Add($"{attachment.FileName} ({attachment.NotIncludedReason ?? "not downloaded"})");
                continue;
            }
            if (!fileStorage.IsConfigured)
            {
                notAttached.Add($"{attachment.FileName} (file storage is not configured)");
                continue;
            }

            try
            {
                using var content = new MemoryStream(attachment.Content, writable: false);
                var file = await fileStorage.UploadAsync(content, attachment.FileName, attachment.ContentType, AttachmentFolder, cancellationToken);
                stored.Add(new TaskAttachment
                {
                    FileName = attachment.FileName,
                    ContentType = attachment.ContentType,
                    SizeBytes = attachment.Content.LongLength,
                    Url = file.Url,
                    PublicId = file.PublicId
                });
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Not retried: the email still counts as processed.
                notAttached.Add($"{attachment.FileName} (upload failed: {ex.Message})");
            }
        }
        return (stored, notAttached);
    }

    private static string Title(string subject)
    {
        var title = subject.Trim();
        return title.Length == 0 ? "(no subject)" : Truncate(title, MailLimits.MaxTitleLength);
    }

    private static string Description(MailboxMessage message, List<string> notAttached)
    {
        var text = new StringBuilder()
            .AppendLine($"From: {message.From}")
            .AppendLine($"Received: {message.ReceivedAt:yyyy-MM-dd HH:mm} UTC")
            .AppendLine()
            .AppendLine(Truncate(message.TextBody.Trim(), MailLimits.MaxBodyLength));
        if (notAttached.Count > 0)
        {
            text.AppendLine().AppendLine("Not attached:");
            foreach (var entry in notAttached)
            {
                text.AppendLine($"- {entry}");
            }
        }
        return text.ToString().TrimEnd();
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];

    /// <summary>Identifies the mailbox (host, user, folder); a different key means a fresh baseline.</summary>
    private static string MailboxKey(MailSettings settings) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{settings.Host.ToLowerInvariant()}|{settings.Username.ToLowerInvariant()}|{settings.Folder}")));

    private async Task<MailPollOutcome> Record(MailSettings settings, int created, string? error, CancellationToken cancellationToken)
    {
        settings.LastPollAt = DateTime.UtcNow;
        settings.LastPollCreated = created;
        settings.LastPollError = error;
        await context.SaveChangesAsync(cancellationToken);
        return new MailPollOutcome(true, settings.PollIntervalSeconds, created, error);
    }
}
