using System.Security.Claims;
using ProjectManager.Api.Extensions;
using ProjectManager.Application.DTOs;
using ProjectManager.Application.Interfaces;

namespace ProjectManager.Api.Endpoints;

public static class ProjectEndpoints
{
    public static IEndpointRouteBuilder MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects")
            .WithTags("Projects")
            .RequireAuthorization();

        // Handlers return IResult, so the response shapes are declared for OpenAPI here.
        group.MapGet("/", GetProjects).Produces<IReadOnlyList<ProjectDto>>();
        group.MapPost("/", CreateProject).Produces<ProjectDto>();
        group.MapDelete("/{id:int}", DeleteProject)
            .Produces<IReadOnlyList<ProjectDto>>()
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> GetProjects(ClaimsPrincipal principal, IProjectService projects) =>
        principal.GetUserId() is { } userId
            ? TypedResults.Ok(await projects.GetAllAsync(userId))
            : TypedResults.Unauthorized();

    private static async Task<IResult> CreateProject(CreateProjectRequest request, ClaimsPrincipal principal, IProjectService projects) =>
        principal.GetUserId() is { } userId
            ? TypedResults.Ok(await projects.CreateAsync(userId, request))
            : TypedResults.Unauthorized();

    private static async Task<IResult> DeleteProject(int id, ClaimsPrincipal principal, IProjectService projects) =>
        principal.GetUserId() is { } userId
            ? (await projects.DeleteAsync(userId, id)).ToHttpResult()
            : TypedResults.Unauthorized();
}
