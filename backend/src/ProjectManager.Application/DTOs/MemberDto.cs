namespace ProjectManager.Application.DTOs;

/// <summary>A project member with every role the user holds in that project. Never carries password data.</summary>
public class MemberDto
{
    public int UserId { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public List<RoleDto> Roles { get; set; } = [];
}

public class AddMemberRequest
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
}
