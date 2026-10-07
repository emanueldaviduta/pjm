using ProjectManager.Application.Common;
using ProjectManager.Application.DTOs;

namespace ProjectManager.Application.Interfaces;

public interface IRoleService
{
    Task<IReadOnlyList<RoleDto>> GetAllAsync();

    /// <returns>NotFound when no user has that id.</returns>
    Task<ServiceResult<IReadOnlyList<RoleDto>>> GetUserRolesAsync(int userId);

    /// <returns>The assigned role; NotFound for an unknown user or role; Conflict when the user already holds it.</returns>
    Task<ServiceResult<RoleDto>> AssignToUserAsync(int userId, int roleId);

    /// <returns>NotFound when the user does not hold that role (or does not exist).</returns>
    Task<ServiceResult> RemoveFromUserAsync(int userId, int roleId);
}
