using ProjectManager.Application.Common;
using ProjectManager.Application.DTOs;

namespace ProjectManager.Application.Interfaces;

/// <summary>
/// Project membership on behalf of a caller. Every method answers NotFound when the caller holds no role
/// in the project (or it does not exist or is deleted); changes also answer Forbidden when the caller is not an Owner.
/// </summary>
public interface IProjectMembershipService
{
    /// <returns>Members grouped per user; any member of the project may list them.</returns>
    Task<ServiceResult<IReadOnlyList<MemberDto>>> GetMembersAsync(int callerId, int projectId);

    /// <returns>The member with all roles held in the project; NotFound for an unknown user or role; Conflict for a duplicate.</returns>
    Task<ServiceResult<MemberDto>> AddRoleAsync(int callerId, int projectId, int userId, int roleId);

    /// <returns>NotFound when the user does not hold that role in the project; Invalid when it is the last Owner role.</returns>
    Task<ServiceResult> RemoveRoleAsync(int callerId, int projectId, int userId, int roleId);

    /// <summary>Drops all of the user's roles in the project; the user, roles and project stay.</summary>
    /// <returns>NotFound when the user is not a member; Invalid when the user is the last Owner.</returns>
    Task<ServiceResult> RemoveMemberAsync(int callerId, int projectId, int userId);
}
