using ProjectManager.Domain.Entities;

namespace ProjectManager.Application.DTOs;

public class TaskItemDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public int? AssignedId { get; set; }
    public string? Assigned { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public Status Status { get; set; }
    public Priority Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>Body of create/update. Only ids are accepted, never nested Project/Assigned objects.</summary>
public class TaskItemRequest
{
    public int ProjectId { get; set; }
    public int? AssignedId { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public Status Status { get; set; }
    public Priority Priority { get; set; }

    // Same default as the entity: omitted => due in a week, explicit null => no due date.
    public DateTime? DueDate { get; set; } = DateTime.UtcNow.AddDays(7);
}
