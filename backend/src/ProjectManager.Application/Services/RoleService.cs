using Microsoft.EntityFrameworkCore;
using ProjectManager.Application.Common;
using ProjectManager.Application.DTOs;
using ProjectManager.Application.Interfaces;
using ProjectManager.Domain.Entities;

namespace ProjectManager.Application.Services;

public class RoleService(IAppDbContext context) : IRoleService
{
    public async Task<IReadOnlyList<RoleDto>> GetAllAsync() =>
        await context.Roles.OrderBy(r => r.Id).Select(r => new RoleDto { Id = r.Id, Name = r.Name }).ToListAsync();

    public async Task<ServiceResult<IReadOnlyList<RoleDto>>> GetUserRolesAsync(int userId)
    {
        if (!await context.AppUsers.AnyAsync(u => u.Id == userId))
        {
            return ServiceResult<IReadOnlyList<RoleDto>>.NotFound("User not found.");
        }

        var roles = await context.UserRoles
            .Where(ur => ur.UserId == userId)
            .OrderBy(ur => ur.RoleId)
            .Select(ur => new RoleDto { Id = ur.Role!.Id, Name = ur.Role.Name })
            .ToListAsync();
        return ServiceResult<IReadOnlyList<RoleDto>>.Ok(roles);
    }

    public async Task<ServiceResult<RoleDto>> AssignToUserAsync(int userId, int roleId)
    {
        if (!await context.AppUsers.AnyAsync(u => u.Id == userId))
        {
            return ServiceResult<RoleDto>.NotFound("User not found.");
        }

        var role = await context.Roles.FindAsync(roleId);
        if (role == null)
        {
            return ServiceResult<RoleDto>.NotFound("Role not found.");
        }

        if (await context.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId))
        {
            return ServiceResult<RoleDto>.Conflict("User already has this role.");
        }

        context.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Two identical requests raced past the check above; the composite key rejected the second.
            return ServiceResult<RoleDto>.Conflict("User already has this role.");
        }

        return ServiceResult<RoleDto>.Ok(new RoleDto { Id = role.Id, Name = role.Name });
    }

    public async Task<ServiceResult> RemoveFromUserAsync(int userId, int roleId)
    {
        var assignment = await context.UserRoles.FindAsync(userId, roleId);
        if (assignment == null)
        {
            return ServiceResult.NotFound("User does not hold this role.");
        }

        context.UserRoles.Remove(assignment);
        await context.SaveChangesAsync();
        return ServiceResult.Ok();
    }
}
