namespace ProjectManager.Application.Interfaces;
using ProjectManager.Application.DTOs;

/// <summary>Tasks on behalf of a user; only tasks of non-deleted projects the user is a member of are reachable.</summary>
public interface ITaskService
{
    Task<IReadOnlyList<TaskItemDto>> GetAllAsync(int userId);

    /// <returns>The task, or null when there is none with that id in the user's projects.</returns>
    Task<TaskItemDto?> GetByIdAsync(int userId, int id);

    /// <returns>The created task, or null when the user is not a member of the target project.</returns>
    Task<TaskItemDto?> CreateAsync(int userId, TaskItemRequest request);

    /// <returns>The updated task, or null when there is none with that id in the user's projects.</returns>
    Task<TaskItemDto?> UpdateAsync(int userId, int id, TaskItemRequest request);
}
