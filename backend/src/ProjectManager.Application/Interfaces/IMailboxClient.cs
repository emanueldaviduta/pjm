using ProjectManager.Domain.Entities;

namespace ProjectManager.Application.Interfaces;

public record MailboxConnection(string Host, int Port, MailSecurity Security, string Username, string Password, string Folder);

public record MailboxTestResult(bool Success, string Message);

/// <summary>
/// A file attached to an email. <see cref="Content"/> is set only when the file is within the limits;
/// otherwise <see cref="NotIncludedReason"/> says why it was not downloaded.
/// </summary>
public record MailboxAttachment(string FileName, string ContentType, long SizeBytes, byte[]? Content, string? NotIncludedReason);

public record MailboxMessage(
    long Uid,
    string? MessageId,
    string From,
    string Subject,
    DateTime? ReceivedAt,
    string TextBody,
    IReadOnlyList<MailboxAttachment> Attachments);

/// <summary>
/// The folder's current UIDVALIDITY and highest UID, and the messages above the cursor. Messages are returned only
/// when <c>uidValidity</c> matches and a cursor exists; otherwise the caller uses the result as a new baseline.
/// </summary>
public record MailboxFetchResult(long UidValidity, long HighestUid, IReadOnlyList<MailboxMessage> Messages);

/// <summary>Read-only access to an IMAP folder. Never sets flags, moves or deletes messages.</summary>
public interface IMailboxClient
{
    /// <summary>Connects, authenticates and opens the folder. Never throws for mailbox problems.</summary>
    Task<MailboxTestResult> TestAsync(MailboxConnection connection, CancellationToken cancellationToken = default);

    Task<MailboxFetchResult> FetchNewAsync(MailboxConnection connection, long? uidValidity, long? lastUid, CancellationToken cancellationToken = default);
}
