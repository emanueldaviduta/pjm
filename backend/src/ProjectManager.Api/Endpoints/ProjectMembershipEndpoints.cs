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

    private static async Task<IResult> GetMembers(int projectId, IProjectMembershipService members) =>
        (await members.GetMembersAsync(projectId)).ToHttpResult();

    private static async Task<IResult> AddMember(int projectId, AddMemberRequest request, IProjectMembershipService members) =>
        (await members.AddRoleAsync(projectId, request.UserId, request.RoleId))
            .ToCreatedHttpResult($"/api/projects/{projectId}/members");

    private static async Task<IResult> RemoveMember(int projectId, int userId, IProjectMembershipService members) =>
        (await members.RemoveMemberAsync(projectId, userId)).ToHttpResult();

    private static async Task<IResult> RemoveRole(int projectId, int userId, int roleId, IProjectMembershipService members) =>
        (await members.RemoveRoleAsync(projectId, userId, roleId)).ToHttpResult();
}
