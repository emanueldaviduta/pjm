using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
            return await Task.FromResult(Ok(context.Projects.ToList()));
        }

        [HttpPost]
        public async Task<ActionResult> CreateProject(Project project)
        {
            // Log the project being created or updated
            Console.WriteLine($"Creating or updating project: {project.Id}");
            if(project.Id == 0 || project.Id == default)
                project.CreatedAt = DateTime.UtcNow;
            else
                project.UpdatedAt = DateTime.UtcNow;

            context.Projects.Add(project);
            context.SaveChanges();

            return await Task.FromResult(Ok(project));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteProject(int id)
        {
            var project = context.Projects.Find(id);
            if (project == null)
            {
                return await Task.FromResult(NotFound());
            }
            project.IsDeleted = true;
            project.UpdatedAt = DateTime.UtcNow;
            context.Projects.Update(project);
            context.SaveChanges();

            return await Task.FromResult(Ok(context.Projects.ToList()));
        }
    }
}
