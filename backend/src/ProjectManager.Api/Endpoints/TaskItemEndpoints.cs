using System.Security.Claims;
using ProjectManager.Api.Extensions;
using ProjectManager.Application.DTOs;
using ProjectManager.Application.Interfaces;

namespace ProjectManager.Api.Endpoints;

public static class TaskItemEndpoints
{
    public static IEndpointRouteBuilder MapTaskItemEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/taskitem")
            .WithTags("TaskItems")
            .RequireAuthorization();

        group.MapGet("/", GetAll);
        group.MapGet("/{id:int}", GetById);
        group.MapPost("/", Create);
        group.MapPut("/{id:int}", Update);

        return app;
    }

    private static async Task<IResult> GetAll(ClaimsPrincipal principal, ITaskService tasks) =>
        principal.GetUserId() is { } userId
            ? TypedResults.Ok(await tasks.GetAllAsync(userId))
            : TypedResults.Unauthorized();

    private static async Task<IResult> GetById(int id, ClaimsPrincipal principal, ITaskService tasks) =>
        principal.GetUserId() is not { } userId ? TypedResults.Unauthorized()
        : await tasks.GetByIdAsync(userId, id) is { } task ? TypedResults.Ok(task)
        : TypedResults.NotFound();

    private static async Task<IResult> Create(TaskItemRequest request, ClaimsPrincipal principal, ITaskService tasks) =>
        principal.GetUserId() is not { } userId ? TypedResults.Unauthorized()
        : await tasks.CreateAsync(userId, request) is { } task ? TypedResults.Ok(task)
        : TypedResults.NotFound();

    private static async Task<IResult> Update(int id, TaskItemRequest request, ClaimsPrincipal principal, ITaskService tasks) =>
        principal.GetUserId() is not { } userId ? TypedResults.Unauthorized()
        : await tasks.UpdateAsync(userId, id, request) is { } task ? TypedResults.Ok(task)
        : TypedResults.NotFound();
}
