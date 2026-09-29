using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Models;
using WebAPI.Data;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectsController(AppDb context) : ControllerBase
    {
        private readonly AppDb _context = context;

        [HttpGet]
        public ActionResult GetProjects()
        {
            return Ok(_context.Projects.ToList());
        }

        [HttpPost]
        public ActionResult CreateProject(Project project)
        {
            if(project.Id == 0)
                project.CreatedAt = DateTime.UtcNow;
            else
                project.UpdatedAt = DateTime.UtcNow;

            _context.Projects.Add(project);
            _context.SaveChanges();

            return Ok(project);
        }

        [HttpDelete("{id}")]
        public ActionResult DeleteProject(int id)
        {
            var project = _context.Projects.Find(id);
            if (project == null)
            {
                return NotFound();
            }
            project.IsDeleted = true;
            project.UpdatedAt = DateTime.UtcNow;
            _context.Projects.Update(project);
            _context.SaveChanges();

            return Ok(_context.Projects.ToList());
        }
    }
}
