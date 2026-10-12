using Microsoft.EntityFrameworkCore;
using ProjectManager.Application.Common;
using ProjectManager.Application.DTOs;
using ProjectManager.Application.Interfaces;
using ProjectManager.Domain.Entities;

namespace ProjectManager.Application.Services;

public class ProjectMembershipService(IAppDbContext context) : IProjectMembershipService
{
    public async Task<ServiceResult<IReadOnlyList<MemberDto>>> GetMembersAsync(int callerId, int projectId)
    {
        if ((await context.RolesInProjectAsync(projectId, callerId)).Count == 0)
        {
            return ServiceResult<IReadOnlyList<MemberDto>>.NotFound("Project not found.");
        }

        return ServiceResult<IReadOnlyList<MemberDto>>.Ok(await LoadMembers(projectId, userId: null));
    }

    public async Task<ServiceResult<MemberDto>> AddRoleAsync(int callerId, int projectId, int userId, int roleId)
    {
        switch (await CheckOwner(callerId, projectId))
        {
            case ServiceStatus.NotFound: return ServiceResult<MemberDto>.NotFound("Project not found.");
            case ServiceStatus.Forbidden: return ServiceResult<MemberDto>.Forbidden();
        }

        if (!await context.AppUsers.AnyAsync(u => u.Id == userId))
        {
            return ServiceResult<MemberDto>.NotFound("User not found.");
        }

        if (!await context.Roles.AnyAsync(r => r.Id == roleId))
        {
            return ServiceResult<MemberDto>.NotFound("Role not found.");
        }

        if (await context.ProjectUserRoles.AnyAsync(p => p.ProjectId == projectId && p.UserId == userId && p.RoleId == roleId))
        {
            return ServiceResult<MemberDto>.Conflict("User already has this role in the project.");
        }

        context.ProjectUserRoles.Add(new ProjectUserRole { ProjectId = projectId, UserId = userId, RoleId = roleId });
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Two identical requests raced past the check above; the composite key rejected the second.
            return ServiceResult<MemberDto>.Conflict("User already has this role in the project.");
        }

        return ServiceResult<MemberDto>.Ok((await LoadMembers(projectId, userId)).Single());
    }

    public async Task<ServiceResult> RemoveRoleAsync(int callerId, int projectId, int userId, int roleId)
    {
        switch (await CheckOwner(callerId, projectId))
        {
            case ServiceStatus.NotFound: return ServiceResult.NotFound("Project not found.");
            case ServiceStatus.Forbidden: return ServiceResult.Forbidden();
        }

        var assignment = await context.ProjectUserRoles.FindAsync(projectId, userId, roleId);
        if (assignment == null)
        {
            return ServiceResult.NotFound("User does not hold this role in the project.");
        }

        if (await IsLastOwner(projectId, userId, roleId))
        {
            return ServiceResult.Invalid(LastOwnerMessage);
        }

        context.ProjectUserRoles.Remove(assignment);
        await context.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> RemoveMemberAsync(int callerId, int projectId, int userId)
    {
        switch (await CheckOwner(callerId, projectId))
        {
            case ServiceStatus.NotFound: return ServiceResult.NotFound("Project not found.");
            case ServiceStatus.Forbidden: return ServiceResult.Forbidden();
        }

        var assignments = await context.ProjectUserRoles
            .Where(p => p.ProjectId == projectId && p.UserId == userId)
            .ToListAsync();
        if (assignments.Count == 0)
        {
            return ServiceResult.NotFound("User is not a member of the project.");
        }

        foreach (var assignment in assignments)
        {
            if (await IsLastOwner(projectId, userId, assignment.RoleId))
            {
                return ServiceResult.Invalid(LastOwnerMessage);
            }
        }

        context.ProjectUserRoles.RemoveRange(assignments);
        await context.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    private const string LastOwnerMessage = "A project must keep at least one owner.";

    /// <returns>NotFound when the caller is not a member (or no such project), Forbidden when not an Owner, otherwise Ok.</returns>
    private async Task<ServiceStatus> CheckOwner(int callerId, int projectId)
    {
        var roles = await context.RolesInProjectAsync(projectId, callerId);
        if (roles.Count == 0) return ServiceStatus.NotFound;
        return roles.Contains(RoleNames.Owner) ? ServiceStatus.Ok : ServiceStatus.Forbidden;
    }

    /// <returns>True when removing this role from the user would leave the project without an Owner.</returns>
    private async Task<bool> IsLastOwner(int projectId, int userId, int roleId) =>
        await context.Roles.AnyAsync(r => r.Id == roleId && r.Name == RoleNames.Owner) &&
        !await context.ProjectUserRoles.AnyAsync(p =>
            p.ProjectId == projectId && p.UserId != userId && p.Role!.Name == RoleNames.Owner);

    // Selects only the fields a MemberDto needs, so password hash and salt are never read.
    private async Task<List<MemberDto>> LoadMembers(int projectId, int? userId)
    {
        var rows = await context.ProjectUserRoles
            .Where(p => p.ProjectId == projectId && (userId == null || p.UserId == userId))
            .OrderBy(p => p.UserId).ThenBy(p => p.RoleId)
            .Select(p => new
            {
                p.UserId,
                p.User!.FirstName,
                p.User.LastName,
                p.User.Email,
                RoleId = p.Role!.Id,
                RoleName = p.Role.Name
            })
            .ToListAsync();

        return rows
            .GroupBy(r => r.UserId)
            .Select(g => new MemberDto
            {
                UserId = g.Key,
                FirstName = g.First().FirstName,
                LastName = g.First().LastName,
                Email = g.First().Email,
                Roles = g.Select(r => new RoleDto { Id = r.RoleId, Name = r.RoleName }).ToList()
            })
            .ToList();
    }
}
