using Microsoft.EntityFrameworkCore;
using ProjectManager.Application.DTOs;
using ProjectManager.Application.Interfaces;
using ProjectManager.Domain.Entities;

namespace ProjectManager.Application.Services;

public class TaskService(IAppDbContext context) : ITaskService
{
    public async Task<IReadOnlyList<TaskItemDto>> GetAllAsync() =>
        await Query().ToListAsync();

    public Task<TaskItemDto?> GetByIdAsync(int id) =>
        Query().FirstOrDefaultAsync(t => t.Id == id);

    public async Task<TaskItemDto> CreateAsync(TaskItemRequest request)
    {
        var taskItem = new TaskItem
        {
            ProjectId = request.ProjectId,
            AssignedId = request.AssignedId,
            Title = request.Title,
            Description = request.Description,
            Status = request.Status,
            Priority = request.Priority,
            DueDate = request.DueDate
        };

        context.TaskItems.Add(taskItem);
        await context.SaveChangesAsync();
        return await Query().FirstAsync(t => t.Id == taskItem.Id);
    }

    public async Task<TaskItemDto?> UpdateAsync(int id, TaskItemRequest request)
    {
        var taskItem = await context.TaskItems.FindAsync(id);
        if (taskItem == null)
        {
            return null;
        }

        taskItem.Title = request.Title;
        taskItem.Description = request.Description;
        taskItem.Status = request.Status;
        taskItem.Priority = request.Priority;
        taskItem.DueDate = request.DueDate;
        taskItem.AssignedId = request.AssignedId;
        taskItem.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return await Query().FirstAsync(t => t.Id == id);
    }

    // Projecting to a DTO avoids the Task -> Project -> Tasks cycle and keeps AppUser
    // (password hash/salt) out of the response.
    private IQueryable<TaskItemDto> Query() =>
        context.TaskItems.Select(t => new TaskItemDto
        {
            Id = t.Id,
            ProjectId = t.ProjectId,
            ProjectName = t.Project != null ? t.Project.Name : null,
            AssignedId = t.AssignedId,
            Assigned = t.Assigned == null ? null : $"{t.Assigned.FirstName} {t.Assigned.LastName}",
            Title = t.Title,
            Description = t.Description,
            Status = t.Status,
            Priority = t.Priority,
            DueDate = t.DueDate,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        });
}
