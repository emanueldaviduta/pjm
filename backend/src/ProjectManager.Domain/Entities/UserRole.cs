namespace ProjectManager.Domain.Entities;

/// <summary>A role held by a user, independent of any project.</summary>
public class UserRole
{
    public int UserId { get; set; }
    public AppUser? User { get; set; }
    public int RoleId { get; set; }
    public Role? Role { get; set; }
}
