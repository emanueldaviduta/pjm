namespace ProjectManager.Application.Common;

/// <summary>Limits for turning emails into tasks.</summary>
public static class MailLimits
{
    public const long MaxAttachmentBytes = 10 * 1024 * 1024;
    public const int MaxAttachmentsPerEmail = 10;
    public const int MaxTitleLength = 200;
    public const int MaxBodyLength = 10_000;
    public const int MinPollIntervalSeconds = 30;
    public const int MaxPollIntervalSeconds = 3600;
}
