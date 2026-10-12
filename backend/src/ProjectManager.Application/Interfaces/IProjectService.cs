using ProjectManager.Application.Common;
using ProjectManager.Application.DTOs;

namespace ProjectManager.Application.Interfaces;

public interface IProjectService
{
    /// <returns>Every non-deleted project the user is a member of, with the user's roles in it.</returns>
    Task<IReadOnlyList<ProjectDto>> GetAllAsync(int userId);

    /// <summary>Creates a project with the user as its Owner, in one save.</summary>
    Task<ProjectDto> CreateAsync(int userId, CreateProjectRequest request);

    /// <summary>Soft-deletes a project; only an Owner may.</summary>
    /// <returns>NotFound when the user is not a member (or no such project); Forbidden when not an Owner; otherwise the user's remaining projects.</returns>
    Task<ServiceResult<IReadOnlyList<ProjectDto>>> DeleteAsync(int userId, int id);
}
