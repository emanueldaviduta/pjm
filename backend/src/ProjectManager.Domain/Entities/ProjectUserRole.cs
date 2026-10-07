namespace ProjectManager.Domain.Entities;

/// <summary>A role a user holds in one project. A user can hold several roles in the same project.</summary>
public class ProjectUserRole
{
    public int ProjectId { get; set; }
    public Project? Project { get; set; }
    public int UserId { get; set; }
    public AppUser? User { get; set; }
    public int RoleId { get; set; }
    public Role? Role { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
