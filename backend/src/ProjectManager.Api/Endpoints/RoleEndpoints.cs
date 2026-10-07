using ProjectManager.Api.Extensions;
using ProjectManager.Application.DTOs;
using ProjectManager.Application.Interfaces;

namespace ProjectManager.Api.Endpoints;

public static class RoleEndpoints
{
    public static IEndpointRouteBuilder MapRoleEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/roles", GetRoles)
            .WithTags("Roles")
            .RequireAuthorization();

        var userRoles = app.MapGroup("/api/account/users/{userId:int}/roles")
            .WithTags("Roles")
            .RequireAuthorization();

        userRoles.MapGet("/", GetUserRoles);
        userRoles.MapPost("/", AssignRole);
        userRoles.MapDelete("/{roleId:int}", RemoveRole);

        return app;
    }

    private static async Task<IResult> GetRoles(IRoleService roles) =>
        TypedResults.Ok(await roles.GetAllAsync());

    private static async Task<IResult> GetUserRoles(int userId, IRoleService roles) =>
        (await roles.GetUserRolesAsync(userId)).ToHttpResult();

    private static async Task<IResult> AssignRole(int userId, AssignRoleRequest request, IRoleService roles) =>
        (await roles.AssignToUserAsync(userId, request.RoleId))
            .ToCreatedHttpResult($"/api/account/users/{userId}/roles");

    private static async Task<IResult> RemoveRole(int userId, int roleId, IRoleService roles) =>
        (await roles.RemoveFromUserAsync(userId, roleId)).ToHttpResult();
}
