namespace ProjectManager.Domain.Entities;

/// <summary>A file attached to a task, stored in external file storage and served from <see cref="Url"/>.</summary>
public class TaskAttachment
{
    public int Id { get; set; }
    public int TaskItemId { get; set; }
    public TaskItem? TaskItem { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public long SizeBytes { get; set; }
    public required string Url { get; set; }
    public required string PublicId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
