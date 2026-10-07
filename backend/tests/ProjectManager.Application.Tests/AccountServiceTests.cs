using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ProjectManager.Application.Common;
using ProjectManager.Application.DTOs;
using ProjectManager.Application.Services;
using ProjectManager.Infrastructure.Persistence;
using ProjectManager.Infrastructure.Services;

namespace ProjectManager.Application.Tests;

/// <summary>
/// Service tests against a real (in-memory) SQLite database, no web host involved.
/// xUnit creates a new instance of this class for every test, so each test gets an empty database,
/// and Dispose() cleans it up afterwards.
/// </summary>
public class AccountServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDb _db;
    private readonly AccountService _service;

    public AccountServiceTests()
    {
        // The in-memory database lives as long as this connection stays open.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _db = new AppDb(new DbContextOptionsBuilder<AppDb>().UseSqlite(_connection).Options);
        _db.Database.EnsureCreated();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["TokenKey"] = TestData.TokenKey })
            .Build();
        _service = new AccountService(_db, new TokenService(config));
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private Task<ServiceResult<UserDto>> RegisterAna() => _service.RegisterAsync(new RegisterDto
    {
        FirstName = "Ana",
        LastName = "Pop",
        Email = "ana@example.com",
        Password = "correct-horse"
    });

    /// <summary>A [Fact] that walks through a small flow (register, then log in).</summary>
    [Fact]
    public async Task Register_ThenLoginWithSamePassword_ReturnsToken()
    {
        await RegisterAna();

        var result = await _service.LoginAsync(new LoginDto { Email = "ana@example.com", Password = "correct-horse" });

        Assert.Equal(ServiceStatus.Ok, result.Status);
        var user = Assert.IsType<UserDto>(result.Value);
        Assert.Equal("ana@example.com", user.Email);
        Assert.False(string.IsNullOrEmpty(user.Token));
        // The password must never be stored in plain text.
        Assert.NotEqual("correct-horse"u8.ToArray(), _db.AppUsers.Single().PasswordHash);
    }

    /// <summary>A [Theory] runs the same test once per InlineData row.</summary>
    [Theory]
    [InlineData("ana@example.com", "wrong-password")]   // known email, wrong password
    [InlineData("nobody@example.com", "correct-horse")] // unknown email
    [InlineData("ANA@example.com", "correct-horse")]    // email match is case-sensitive today
    public async Task Login_WithWrongCredentials_ReturnsInvalid(string email, string password)
    {
        await RegisterAna();

        var result = await _service.LoginAsync(new LoginDto { Email = email, Password = password });

        Assert.Equal(ServiceStatus.Invalid, result.Status);
        // Same message for both cases, so the response does not reveal which emails exist.
        Assert.Equal("Invalid username or password.", result.Message);
    }

    [Fact]
    public async Task Register_WithExistingEmail_ReturnsInvalid()
    {
        await RegisterAna();

        var result = await RegisterAna();

        Assert.Equal(ServiceStatus.Invalid, result.Status);
        Assert.Equal("User with this email already exists.", result.Message);
        Assert.Single(_db.AppUsers);
    }

    [Fact]
    public async Task UpdateMe_TrimsNames_AndUnknownUserIsUnauthorized()
    {
        await RegisterAna();
        var id = _db.AppUsers.Single().Id;

        var updated = await _service.UpdateMeAsync(id, new UpdateAccountDto { FirstName = " Ana2 ", LastName = " Pop2 " });
        var missing = await _service.UpdateMeAsync(999, new UpdateAccountDto { FirstName = "x", LastName = "y" });

        Assert.Equal(ServiceStatus.Ok, updated.Status);
        Assert.Equal("Ana2", updated.Value!.FirstName);
        Assert.Equal("Pop2", updated.Value.LastName);
        Assert.Equal(ServiceStatus.Unauthorized, missing.Status);
    }

    [Fact]
    public async Task GetMe_UnknownUser_IsUnauthorized()
    {
        var result = await _service.GetMeAsync(999);

        Assert.Equal(ServiceStatus.Unauthorized, result.Status);
    }

    [Fact]
    public async Task ChangePassword_WithWrongCurrentPassword_ReturnsInvalid_AndKeepsOldPassword()
    {
        await RegisterAna();
        var id = _db.AppUsers.Single().Id;

        var result = await _service.ChangePasswordAsync(id,
            new ChangePasswordDto { CurrentPassword = "wrong-wrong", NewPassword = "another-pass" });
        var login = await _service.LoginAsync(new LoginDto { Email = "ana@example.com", Password = "correct-horse" });

        Assert.Equal(ServiceStatus.Invalid, result.Status);
        Assert.Equal("Current password is incorrect.", result.Message);
        Assert.Equal(ServiceStatus.Ok, login.Status);
    }

    [Fact]
    public async Task ChangePassword_Succeeds_ThenOnlyNewPasswordLogsIn()
    {
        await RegisterAna();
        var id = _db.AppUsers.Single().Id;

        var result = await _service.ChangePasswordAsync(id,
            new ChangePasswordDto { CurrentPassword = "correct-horse", NewPassword = "another-pass" });
        var oldLogin = await _service.LoginAsync(new LoginDto { Email = "ana@example.com", Password = "correct-horse" });
        var newLogin = await _service.LoginAsync(new LoginDto { Email = "ana@example.com", Password = "another-pass" });

        Assert.Equal(ServiceStatus.Ok, result.Status);
        Assert.Equal(ServiceStatus.Invalid, oldLogin.Status);
        Assert.Equal(ServiceStatus.Ok, newLogin.Status);
    }

    [Fact]
    public async Task ChangePassword_UnknownUser_IsUnauthorized()
    {
        var result = await _service.ChangePasswordAsync(999,
            new ChangePasswordDto { CurrentPassword = "a", NewPassword = "another-pass" });

        Assert.Equal(ServiceStatus.Unauthorized, result.Status);
    }
}
