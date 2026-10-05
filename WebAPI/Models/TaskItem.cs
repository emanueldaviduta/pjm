
namespace WebAPI.Models;
public class TaskItem
{
    public int Id { get; set; }
    public required int ProjectId { get; set; }
    public required int AssignedId { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }

    public required Status Status { get; set; }
    public required Priority Priority { get; set; }
    public DateTime? DueDate { get; set; } = DateTime.UtcNow.AddDays(7);
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum Status
{
    Created,
    InProgress,
    Completed,
    OnHold
}

public enum Priority
{
    Low,
    Medium,
    High,
    Critical
}