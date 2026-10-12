using System.Security.Claims;
using ProjectManager.Api.Extensions;
using ProjectManager.Application.DTOs;
using ProjectManager.Application.Interfaces;

namespace ProjectManager.Api.Endpoints;

/// <summary>Admin-only settings. The service answers Forbidden for non-admins.</summary>
public static class SettingsEndpoints
{
    public static IEndpointRouteBuilder MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/settings")
            .WithTags("Settings")
            .RequireAuthorization();

        // Handlers return IResult, so the response shapes are declared for OpenAPI here.
        group.MapGet("/users", GetUsers).Produces<IReadOnlyList<SettingsUserDto>>().Produces(StatusCodes.Status403Forbidden);
        group.MapGet("/projects", GetProjects).Produces<IReadOnlyList<ProjectOptionDto>>().Produces(StatusCodes.Status403Forbidden);
        group.MapGet("/mail", GetMail).Produces<MailSettingsDto>().Produces(StatusCodes.Status403Forbidden);
        group.MapPut("/mail", SaveMail).Produces<MailSettingsDto>()
            .Produces<string>(StatusCodes.Status400BadRequest).Produces(StatusCodes.Status403Forbidden);
        group.MapPost("/mail/test", TestMail).Produces<MailTestResultDto>().Produces(StatusCodes.Status403Forbidden);

        return app;
    }

    private static async Task<IResult> GetUsers(ClaimsPrincipal principal, IMailSettingsService settings) =>
        principal.GetUserId() is { } callerId
            ? (await settings.GetUsersAsync(callerId)).ToHttpResult()
            : TypedResults.Unauthorized();

    private static async Task<IResult> GetProjects(ClaimsPrincipal principal, IMailSettingsService settings) =>
        principal.GetUserId() is { } callerId
            ? (await settings.GetProjectsAsync(callerId)).ToHttpResult()
            : TypedResults.Unauthorized();

    private static async Task<IResult> GetMail(ClaimsPrincipal principal, IMailSettingsService settings) =>
        principal.GetUserId() is { } callerId
            ? (await settings.GetAsync(callerId)).ToHttpResult()
            : TypedResults.Unauthorized();

    private static async Task<IResult> SaveMail(SaveMailSettingsRequest request, ClaimsPrincipal principal, IMailSettingsService settings) =>
        principal.GetUserId() is { } callerId
            ? (await settings.SaveAsync(callerId, request)).ToHttpResult()
            : TypedResults.Unauthorized();

    private static async Task<IResult> TestMail(SaveMailSettingsRequest request, ClaimsPrincipal principal, IMailSettingsService settings) =>
        principal.GetUserId() is { } callerId
            ? (await settings.TestAsync(callerId, request)).ToHttpResult()
            : TypedResults.Unauthorized();
}
