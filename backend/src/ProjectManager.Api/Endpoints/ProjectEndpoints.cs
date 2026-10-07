using ProjectManager.Api.Extensions;
using ProjectManager.Application.Interfaces;
using ProjectManager.Domain.Entities;

namespace ProjectManager.Api.Endpoints;

public static class ProjectEndpoints
{
    public static IEndpointRouteBuilder MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects")
            .WithTags("Projects");
            // .RequireAuthorization();

        group.MapGet("/", GetProjects);
        group.MapPost("/", CreateProject);
        group.MapDelete("/{id:int}", DeleteProject);

        return app;
    }

    private static async Task<IResult> GetProjects(IProjectService projects) =>
        TypedResults.Ok(await projects.GetAllAsync());

    private static async Task<IResult> CreateProject(Project project, IProjectService projects) =>
        TypedResults.Ok(await projects.CreateAsync(project));

    private static async Task<IResult> DeleteProject(int id, IProjectService projects) =>
        (await projects.DeleteAsync(id)).ToHttpResult();
}
