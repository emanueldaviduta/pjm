namespace ProjectManager.Application.Interfaces;

/// <summary>Outcome of one poll, for the background worker: whether it ran, when to poll next, and what happened.</summary>
public record MailPollOutcome(bool Enabled, int PollIntervalSeconds, int Created, string? Error);

/// <summary>Turns new emails from allowed senders into tasks on the configured project.</summary>
public interface IEmailIngestionService
{
    /// <summary>Runs one poll. Mailbox and storage problems are recorded in the settings, not thrown.</summary>
    Task<MailPollOutcome> PollOnceAsync(CancellationToken cancellationToken = default);
}
