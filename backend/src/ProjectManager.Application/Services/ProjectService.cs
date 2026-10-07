using Microsoft.EntityFrameworkCore;
using ProjectManager.Application.Common;
using ProjectManager.Application.Interfaces;
using ProjectManager.Domain.Entities;

namespace ProjectManager.Application.Services;

public class ProjectService(IAppDbContext context) : IProjectService
{
    public async Task<IReadOnlyList<Project>> GetAllAsync() =>
        await context.Projects.Where(p => !p.IsDeleted).ToListAsync();

    public async Task<Project> CreateAsync(Project project)
    {
        if (project.Id == 0)
            project.CreatedAt = DateTime.UtcNow;
        else
            project.UpdatedAt = DateTime.UtcNow;

        context.Projects.Add(project);
        await context.SaveChangesAsync();

        return project;
    }

    public async Task<ServiceResult<IReadOnlyList<Project>>> DeleteAsync(int id)
    {
        var project = await context.Projects.FindAsync(id);
        if (project == null)
        {
            return ServiceResult<IReadOnlyList<Project>>.NotFound();
        }
        project.IsDeleted = true;
        project.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        return ServiceResult<IReadOnlyList<Project>>.Ok(await context.Projects.ToListAsync());
    }
}
