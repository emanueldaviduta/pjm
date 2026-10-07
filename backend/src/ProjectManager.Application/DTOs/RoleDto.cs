namespace ProjectManager.Application.DTOs;

public class RoleDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
}

public class AssignRoleRequest
{
    public int RoleId { get; set; }
}
