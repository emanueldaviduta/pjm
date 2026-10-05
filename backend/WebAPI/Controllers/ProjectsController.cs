using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPI.Models;
using WebAPI.Data;
using Microsoft.AspNetCore.Authorization;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProjectsController(AppDb context) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult> GetProjects()
        {
            return Ok( await context.Projects.ToListAsync());
        }

        [HttpPost]
        public async Task<ActionResult> CreateProject(Project project)
        {
            if(project.Id == 0)
                project.CreatedAt = DateTime.UtcNow;
            else
                project.UpdatedAt = DateTime.UtcNow;

            context.Projects.Add(project);
            await context.SaveChangesAsync();

            return Ok(project);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteProject(int id)
        {
            var project = await context.Projects.FindAsync(id);
            if (project == null)
            {
                return NotFound();
            }
            project.IsDeleted = true;
            project.UpdatedAt = DateTime.UtcNow;
            context.Projects.Update(project);
            await context.SaveChangesAsync();

            return Ok(await context.Projects.ToListAsync());
        }
    }
}
