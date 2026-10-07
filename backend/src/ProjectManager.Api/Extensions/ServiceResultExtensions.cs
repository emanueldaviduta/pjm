using System.Security.Claims;
using ProjectManager.Application.Common;

namespace ProjectManager.Api.Extensions;

public static class ServiceResultExtensions
{
    /// <summary>Maps a service outcome to HTTP: Ok is 200 with the value.</summary>
    public static IResult ToHttpResult<T>(this ServiceResult<T> result) => result.Status switch
    {
        ServiceStatus.Ok => TypedResults.Ok(result.Value),
        _ => FromFailure(result.Status, result.Message)
    };

    /// <summary>Maps a service outcome that created something: Ok is 201 with the value.</summary>
    public static IResult ToCreatedHttpResult<T>(this ServiceResult<T> result, string uri) => result.Status switch
    {
        ServiceStatus.Ok => TypedResults.Created(uri, result.Value),
        _ => FromFailure(result.Status, result.Message)
    };

    /// <summary>Maps a service outcome that has no value: Ok is 204.</summary>
    public static IResult ToHttpResult(this ServiceResult result) => result.Status switch
    {
        ServiceStatus.Ok => TypedResults.NoContent(),
        _ => FromFailure(result.Status, result.Message)
    };

    /// <summary>The id of the signed-in user from the token, or null when the claim is missing or invalid.</summary>
    public static int? GetUserId(this ClaimsPrincipal principal) =>
        int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null;

    private static IResult FromFailure(ServiceStatus status, string? message) => status switch
    {
        ServiceStatus.NotFound => TypedResults.NotFound(),
        ServiceStatus.Conflict => TypedResults.Conflict(message),
        ServiceStatus.Invalid => TypedResults.BadRequest(message),
        ServiceStatus.Unauthorized => TypedResults.Unauthorized(),
        _ => TypedResults.StatusCode(StatusCodes.Status500InternalServerError)
    };
}
