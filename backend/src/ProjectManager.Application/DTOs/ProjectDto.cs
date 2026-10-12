using System.ComponentModel.DataAnnotations;

namespace ProjectManager.Application.DTOs;

/// <summary>A project as seen by the caller, with the names of the roles the caller holds in it.</summary>
public class ProjectDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public List<string> Roles { get; set; } = [];
}

/// <summary>Body of create. Id, timestamps and deletion are set by the server.</summary>
public class CreateProjectRequest
{
    [Required]
    public required string Name { get; set; }
    public string Description { get; set; } = "";
}
