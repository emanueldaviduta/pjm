using ProjectManager.Application.Common;
using ProjectManager.Application.DTOs;

namespace ProjectManager.Application.Interfaces;

public interface IProjectMembershipService
{
    /// <returns>Members grouped per user; NotFound for an unknown or deleted project.</returns>
    Task<ServiceResult<IReadOnlyList<MemberDto>>> GetMembersAsync(int projectId);

    /// <returns>The member with all roles held in the project; NotFound for an unknown or deleted project, user or role; Conflict for a duplicate.</returns>
    Task<ServiceResult<MemberDto>> AddRoleAsync(int projectId, int userId, int roleId);

    /// <returns>NotFound for an unknown or deleted project or when the user does not hold that role in it.</returns>
    Task<ServiceResult> RemoveRoleAsync(int projectId, int userId, int roleId);

    /// <summary>Drops all of the user's roles in the project; the user, roles and project stay.</summary>
    /// <returns>NotFound for an unknown or deleted project or when the user is not a member.</returns>
    Task<ServiceResult> RemoveMemberAsync(int projectId, int userId);
}
