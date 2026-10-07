using ProjectManager.Application.Common;
using ProjectManager.Domain.Entities;

namespace ProjectManager.Application.Interfaces;

public interface IProjectService
{
    /// <returns>Every project that is not soft-deleted.</returns>
    Task<IReadOnlyList<Project>> GetAllAsync();

    Task<Project> CreateAsync(Project project);

    /// <summary>Soft-deletes a project.</summary>
    /// <returns>NotFound when no project has that id; otherwise every project, as the endpoint returned before.</returns>
    Task<ServiceResult<IReadOnlyList<Project>>> DeleteAsync(int id);
}
