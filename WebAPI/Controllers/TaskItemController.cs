using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPI.Data;
using WebAPI.Models;

namespace MyApp.Namespace
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TaskItemController(AppDb context) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            // Logic to retrieve all task items
            var taskItems = await context.Set<TaskItem>().ToListAsync();
            return await Task.FromResult(Ok(taskItems));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            // Logic to retrieve a task item by its ID
            var taskItem = await context.Set<TaskItem>().FindAsync(id);
            if (taskItem == null)
            {
                return await Task.FromResult(NotFound() as IActionResult);
            }
            return await Task.FromResult(Ok(taskItem) as IActionResult);
        }

        [HttpPost]
        public async Task<IActionResult> Create(TaskItem taskItem)
        {
            context.Set<TaskItem>().Add(taskItem);
            await context.SaveChangesAsync();
            return await Task.FromResult(Ok(taskItem) as IActionResult);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, TaskItem changes)
        {
            var taskItem = await context.Set<TaskItem>().FindAsync(id);
            if (taskItem == null)
            {
                return NotFound();
            }

            taskItem.Title = changes.Title;
            taskItem.Description = changes.Description;
            taskItem.Status = changes.Status;
            taskItem.Priority = changes.Priority;
            taskItem.DueDate = changes.DueDate;
            taskItem.AssignedId = changes.AssignedId;
            taskItem.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            return Ok(taskItem);
        }
    }
}
