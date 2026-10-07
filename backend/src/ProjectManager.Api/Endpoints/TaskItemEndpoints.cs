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

    private static async Task<IResult> GetAll(ITaskService tasks) =>
        TypedResults.Ok(await tasks.GetAllAsync());

    private static async Task<IResult> GetById(int id, ITaskService tasks) =>
        await tasks.GetByIdAsync(id) is { } task ? TypedResults.Ok(task) : TypedResults.NotFound();

    private static async Task<IResult> Create(TaskItemRequest request, ITaskService tasks) =>
        TypedResults.Ok(await tasks.CreateAsync(request));

    private static async Task<IResult> Update(int id, TaskItemRequest request, ITaskService tasks) =>
        await tasks.UpdateAsync(id, request) is { } task ? TypedResults.Ok(task) : TypedResults.NotFound();
}
