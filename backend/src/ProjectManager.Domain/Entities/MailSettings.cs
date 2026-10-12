namespace ProjectManager.Domain.Entities;

/// <summary>
/// The inbound mailbox whose emails become tasks. There is a single row (Id 1); the password is stored
/// encrypted and the cursor fields track which messages have been seen.
/// </summary>
public class MailSettings
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;
    public bool Enabled { get; set; }
    public string Host { get; set; } = "";
    public int Port { get; set; } = 993;
    public MailSecurity Security { get; set; } = MailSecurity.Auto;
    public string Username { get; set; } = "";
    public string? ProtectedPassword { get; set; }
    public string Folder { get; set; } = "INBOX";
    public List<string> AllowedSenders { get; set; } = [];
    public int? ProjectId { get; set; }
    public Project? Project { get; set; }
    public int PollIntervalSeconds { get; set; } = 60;

    // Cursor: which mailbox it belongs to and the highest UID already examined.
    public string? MailboxKey { get; set; }
    public long? UidValidity { get; set; }
    public long? LastUid { get; set; }

    // Outcome of the last poll.
    public DateTime? LastPollAt { get; set; }
    public string? LastPollError { get; set; }
    public int? LastPollCreated { get; set; }
}

public enum MailSecurity
{
    Auto,
    SslOnConnect,
    StartTls,
    None
}
