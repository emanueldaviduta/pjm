using ProjectManager.Application.Common;
using ProjectManager.Application.DTOs;

namespace ProjectManager.Application.Interfaces;

/// <summary>The role catalog, and users' global roles; managing global roles is admin-only (Forbidden otherwise).</summary>
public interface IRoleService
{
    Task<IReadOnlyList<RoleDto>> GetAllAsync();

    /// <returns>Forbidden when the caller is not an admin; NotFound when no user has that id.</returns>
    Task<ServiceResult<IReadOnlyList<RoleDto>>> GetUserRolesAsync(int callerId, int userId);

    /// <returns>The assigned role; Forbidden when the caller is not an admin; NotFound for an unknown user or role; Conflict when the user already holds it.</returns>
    Task<ServiceResult<RoleDto>> AssignToUserAsync(int callerId, int userId, int roleId);

    /// <returns>Forbidden when the caller is not an admin; NotFound when the user does not hold that role (or does not exist); Invalid when it is the last Admin.</returns>
    Task<ServiceResult> RemoveFromUserAsync(int callerId, int userId, int roleId);
}
