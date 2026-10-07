using System.Security.Claims;
using ProjectManager.Api.Extensions;
using ProjectManager.Application.DTOs;
using ProjectManager.Application.Interfaces;

namespace ProjectManager.Api.Endpoints;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/account")
            .WithTags("Account");

        group.MapPost("/login", Login);
        group.MapPost("/register", Register);

        group.MapGet("/users", GetUsers).RequireAuthorization();
        group.MapGet("/me", GetMe).RequireAuthorization();
        group.MapPut("/me", UpdateMe).RequireAuthorization();
        group.MapPut("/password", ChangePassword).RequireAuthorization();

        return app;
    }

    private static async Task<IResult> GetUsers(IAccountService accounts) =>
        TypedResults.Ok(await accounts.GetUsersAsync());

    private static async Task<IResult> Login(LoginDto loginDto, IAccountService accounts) =>
        (await accounts.LoginAsync(loginDto)).ToHttpResult();

    private static async Task<IResult> Register(RegisterDto registerDto, IAccountService accounts) =>
        (await accounts.RegisterAsync(registerDto)).ToHttpResult();

    private static async Task<IResult> GetMe(ClaimsPrincipal principal, IAccountService accounts) =>
        principal.GetUserId() is { } userId
            ? (await accounts.GetMeAsync(userId)).ToHttpResult()
            : TypedResults.Unauthorized();

    private static async Task<IResult> UpdateMe(UpdateAccountDto updateDto, ClaimsPrincipal principal, IAccountService accounts) =>
        principal.GetUserId() is { } userId
            ? (await accounts.UpdateMeAsync(userId, updateDto)).ToHttpResult()
            : TypedResults.Unauthorized();

    private static async Task<IResult> ChangePassword(ChangePasswordDto passwordDto, ClaimsPrincipal principal, IAccountService accounts) =>
        principal.GetUserId() is { } userId
            ? (await accounts.ChangePasswordAsync(userId, passwordDto)).ToHttpResult()
            : TypedResults.Unauthorized();
}
