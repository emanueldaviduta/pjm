using System.Security.Claims;
using ProjectManager.Api.Extensions;
using ProjectManager.Application.DTOs;
using ProjectManager.Application.Interfaces;

namespace ProjectManager.Api.Endpoints;

public static class ProjectMembershipEndpoints
{
    public static IEndpointRouteBuilder MapProjectMembershipEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects/{projectId:int}/members")
            .WithTags("Project members")
            .RequireAuthorization();

        group.MapGet("/", GetMembers);
        group.MapPost("/", AddMember);
        group.MapDelete("/{userId:int}", RemoveMember);
        group.MapDelete("/{userId:int}/roles/{roleId:int}", RemoveRole);

        return app;
    }

    private static async Task<IResult> GetMembers(int projectId, ClaimsPrincipal principal, IProjectMembershipService members) =>
        principal.GetUserId() is { } callerId
            ? (await members.GetMembersAsync(callerId, projectId)).ToHttpResult()
            : TypedResults.Unauthorized();

    private static async Task<IResult> AddMember(int projectId, AddMemberRequest request, ClaimsPrincipal principal, IProjectMembershipService members) =>
        principal.GetUserId() is { } callerId
            ? (await members.AddRoleAsync(callerId, projectId, request.UserId, request.RoleId))
                .ToCreatedHttpResult($"/api/projects/{projectId}/members")
            : TypedResults.Unauthorized();

    private static async Task<IResult> RemoveMember(int projectId, int userId, ClaimsPrincipal principal, IProjectMembershipService members) =>
        principal.GetUserId() is { } callerId
            ? (await members.RemoveMemberAsync(callerId, projectId, userId)).ToHttpResult()
            : TypedResults.Unauthorized();

    private static async Task<IResult> RemoveRole(int projectId, int userId, int roleId, ClaimsPrincipal principal, IProjectMembershipService members) =>
        principal.GetUserId() is { } callerId
            ? (await members.RemoveRoleAsync(callerId, projectId, userId, roleId)).ToHttpResult()
            : TypedResults.Unauthorized();
}
