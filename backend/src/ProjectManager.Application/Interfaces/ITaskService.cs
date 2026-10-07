namespace ProjectManager.Application.Interfaces;
using ProjectManager.Application.DTOs;

public interface ITaskService
{
    Task<IReadOnlyList<TaskItemDto>> GetAllAsync();
    Task<TaskItemDto?> GetByIdAsync(int id);
    Task<TaskItemDto> CreateAsync(TaskItemRequest request);

    /// <returns>The updated task, or null when no task has that id.</returns>
    Task<TaskItemDto?> UpdateAsync(int id, TaskItemRequest request);
}
