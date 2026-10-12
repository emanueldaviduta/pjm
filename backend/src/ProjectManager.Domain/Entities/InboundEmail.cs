namespace ProjectManager.Domain.Entities;

/// <summary>A message already examined by email ingestion, so it is never processed twice.</summary>
public class InboundEmail
{
    public int Id { get; set; }
    public required string MailboxKey { get; set; }
    /// <summary>The Message-ID header, or "uid:{UidValidity}:{Uid}" when it is missing.</summary>
    public required string MessageKey { get; set; }
    public string From { get; set; } = "";
    public string Subject { get; set; } = "";
    public DateTime? ReceivedAt { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    public InboundEmailOutcome Outcome { get; set; }
    public int? TaskItemId { get; set; }
    public TaskItem? TaskItem { get; set; }
}

public enum InboundEmailOutcome
{
    Created,
    Skipped
}
