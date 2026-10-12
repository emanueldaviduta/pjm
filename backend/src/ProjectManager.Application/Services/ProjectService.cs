using Microsoft.EntityFrameworkCore;
using ProjectManager.Application.Common;
using ProjectManager.Application.DTOs;
using ProjectManager.Application.Interfaces;
using ProjectManager.Domain.Entities;

namespace ProjectManager.Application.Services;

public class ProjectService(IAppDbContext context) : IProjectService
{
    public async Task<IReadOnlyList<ProjectDto>> GetAllAsync(int userId) =>
        await Query(userId).ToListAsync();

    public async Task<ProjectDto> CreateAsync(int userId, CreateProjectRequest request)
    {
        var ownerRoleId = await context.Roles.Where(r => r.Name == RoleNames.Owner).Select(r => r.Id).SingleAsync();
        var project = new Project
        {
            Name = request.Name,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Added through the navigation so the project and its owner are stored in one save, or neither is.
        context.ProjectUserRoles.Add(new ProjectUserRole { Project = project, UserId = userId, RoleId = ownerRoleId });
        await context.SaveChangesAsync();

        return await Query(userId).FirstAsync(p => p.Id == project.Id);
    }

    public async Task<ServiceResult<IReadOnlyList<ProjectDto>>> DeleteAsync(int userId, int id)
    {
        var roles = await context.RolesInProjectAsync(id, userId);
        if (roles.Count == 0)
        {
            return ServiceResult<IReadOnlyList<ProjectDto>>.NotFound();
        }
        if (!roles.Contains(RoleNames.Owner))
        {
            return ServiceResult<IReadOnlyList<ProjectDto>>.Forbidden();
        }

        var project = await context.Projects.FindAsync(id);
        project!.IsDeleted = true;
        project.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        return ServiceResult<IReadOnlyList<ProjectDto>>.Ok(await GetAllAsync(userId));
    }

    private IQueryable<ProjectDto> Query(int userId) =>
        context.VisibleProjects(userId).Select(p => new ProjectDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            IsDeleted = p.IsDeleted,
            Roles = context.ProjectUserRoles
                .Where(r => r.ProjectId == p.Id && r.UserId == userId)
                .OrderBy(r => r.RoleId)
                .Select(r => r.Role!.Name)
                .ToList()
        });
}
