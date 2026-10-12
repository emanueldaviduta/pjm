using Microsoft.EntityFrameworkCore;
using ProjectManager.Application.Interfaces;
using ProjectManager.Domain.Entities;

namespace ProjectManager.Application.Common;

public static class RoleNames
{
    public const string Owner = "Owner";
}

/// <summary>Who may see and act on a project, based on the roles the caller holds in it.</summary>
public static class ProjectAccess
{
    /// <returns>Non-deleted projects in which the user holds at least one role.</returns>
    public static IQueryable<Project> VisibleProjects(this IAppDbContext context, int userId) =>
        context.Projects.Where(p => !p.IsDeleted &&
            context.ProjectUserRoles.Any(r => r.ProjectId == p.Id && r.UserId == userId));

    /// <returns>
    /// Names of the roles the user holds in a non-deleted project. Empty when the user is not a member
    /// or the project does not exist; callers answer NotFound for both, so non-members learn nothing.
    /// </returns>
    public static async Task<IReadOnlySet<string>> RolesInProjectAsync(this IAppDbContext context, int projectId, int userId) =>
        (await context.ProjectUserRoles
            .Where(r => r.ProjectId == projectId && r.UserId == userId && !r.Project!.IsDeleted)
            .Select(r => r.Role!.Name)
            .ToListAsync())
        .ToHashSet();
}
