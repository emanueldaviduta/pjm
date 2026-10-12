using ProjectManager.Domain.Entities;

namespace ProjectManager.Application.DTOs;

/// <summary>Inbound mailbox settings as shown to admins. The password is never included, only whether one is set.</summary>
public class MailSettingsDto
{
    public bool Enabled { get; set; }
    public string Host { get; set; } = "";
    public int Port { get; set; }
    public MailSecurity Security { get; set; }
    public string Username { get; set; } = "";
    public bool HasPassword { get; set; }
    public string Folder { get; set; } = "";
    public List<string> AllowedSenders { get; set; } = [];
    public int? ProjectId { get; set; }
    public int PollIntervalSeconds { get; set; }
    public DateTime? LastPollAt { get; set; }
    public string? LastPollError { get; set; }
    public int? LastPollCreated { get; set; }
    public bool AttachmentStorageConfigured { get; set; }
}

/// <summary>Body of save and test. An empty or missing password keeps the stored one.</summary>
public class SaveMailSettingsRequest
{
    public bool Enabled { get; set; }
    public string Host { get; set; } = "";
    public int Port { get; set; } = 993;
    public MailSecurity Security { get; set; } = MailSecurity.Auto;
    public string Username { get; set; } = "";
    public string? Password { get; set; }
    public string Folder { get; set; } = "INBOX";
    public List<string> AllowedSenders { get; set; } = [];
    public int? ProjectId { get; set; }
    public int PollIntervalSeconds { get; set; } = 60;
}

public class MailTestResultDto
{
    public bool Success { get; set; }
    public required string Message { get; set; }
}

/// <summary>A user as listed on the admin Users page.</summary>
public class SettingsUserDto
{
    public int Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public bool IsAdmin { get; set; }
}

/// <summary>A project an admin can pick as the target of email ingestion.</summary>
public class ProjectOptionDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
}
