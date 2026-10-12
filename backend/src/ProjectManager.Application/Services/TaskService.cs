using Microsoft.EntityFrameworkCore;
using ProjectManager.Application.Common;
using ProjectManager.Application.DTOs;
using ProjectManager.Application.Interfaces;
using ProjectManager.Domain.Entities;

namespace ProjectManager.Application.Services;

public class TaskService(IAppDbContext context) : ITaskService
{
    public async Task<IReadOnlyList<TaskItemDto>> GetAllAsync(int userId) =>
        await Query(userId).ToListAsync();

    public Task<TaskItemDto?> GetByIdAsync(int userId, int id) =>
        Query(userId).FirstOrDefaultAsync(t => t.Id == id);

    public async Task<TaskItemDto?> CreateAsync(int userId, TaskItemRequest request)
    {
        if (!await context.VisibleProjects(userId).AnyAsync(p => p.Id == request.ProjectId))
        {
            return null;
        }

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
        return await Query(userId).FirstAsync(t => t.Id == taskItem.Id);
    }

    public async Task<TaskItemDto?> UpdateAsync(int userId, int id, TaskItemRequest request)
    {
        var visible = context.VisibleProjects(userId);
        var taskItem = await context.TaskItems.FirstOrDefaultAsync(t => t.Id == id && visible.Any(p => p.Id == t.ProjectId));
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
        return await Query(userId).FirstAsync(t => t.Id == id);
    }

    // Projecting to a DTO avoids the Task -> Project -> Tasks cycle and keeps AppUser
    // (password hash/salt) out of the response. Only tasks of projects the user is a member of are included.
    private IQueryable<TaskItemDto> Query(int userId)
    {
        var visible = context.VisibleProjects(userId);
        return context.TaskItems.Where(t => visible.Any(p => p.Id == t.ProjectId)).Select(t => new TaskItemDto
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
            UpdatedAt = t.UpdatedAt,
            Attachments = t.Attachments
                .OrderBy(a => a.Id)
                .Select(a => new TaskAttachmentDto
                {
                    Id = a.Id,
                    FileName = a.FileName,
                    ContentType = a.ContentType,
                    SizeBytes = a.SizeBytes,
                    Url = a.Url
                })
                .ToList()
        });
    }
}
