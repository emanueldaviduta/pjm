using System.Security.Claims;
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

    private static async Task<IResult> GetUserRoles(int userId, ClaimsPrincipal principal, IRoleService roles) =>
        principal.GetUserId() is { } callerId
            ? (await roles.GetUserRolesAsync(callerId, userId)).ToHttpResult()
            : TypedResults.Unauthorized();

    private static async Task<IResult> AssignRole(int userId, AssignRoleRequest request, ClaimsPrincipal principal, IRoleService roles) =>
        principal.GetUserId() is { } callerId
            ? (await roles.AssignToUserAsync(callerId, userId, request.RoleId))
                .ToCreatedHttpResult($"/api/account/users/{userId}/roles")
            : TypedResults.Unauthorized();

    private static async Task<IResult> RemoveRole(int userId, int roleId, ClaimsPrincipal principal, IRoleService roles) =>
        principal.GetUserId() is { } callerId
            ? (await roles.RemoveFromUserAsync(callerId, userId, roleId)).ToHttpResult()
            : TypedResults.Unauthorized();
}
