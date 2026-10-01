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
        public ActionResult GetProjects()
        {
            return Ok(context.Projects.ToList());
        }

        [HttpPost]
        public ActionResult CreateProject(Project project)
        {
            if(project.Id == 0)
                project.CreatedAt = DateTime.UtcNow;
            else
                project.UpdatedAt = DateTime.UtcNow;

            context.Projects.Add(project);
            context.SaveChanges();

            return Ok(project);
        }

        [HttpDelete("{id}")]
        public ActionResult DeleteProject(int id)
        {
            var project = context.Projects.Find(id);
            if (project == null)
            {
                return NotFound();
            }
            project.IsDeleted = true;
            project.UpdatedAt = DateTime.UtcNow;
            context.Projects.Update(project);
            context.SaveChanges();

            return Ok(context.Projects.ToList());
        }
    }
}
