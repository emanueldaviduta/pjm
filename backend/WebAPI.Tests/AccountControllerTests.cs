using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WebAPI.Controllers;
using WebAPI.Data;
using WebAPI.DTOs;
using WebAPI.Services;

namespace WebAPI.Tests;

/// <summary>
/// Examples 2 and 3: controller tests against a real (in-memory) SQLite database.
/// xUnit creates a new instance of this class for every test, so each test gets an empty database,
/// and Dispose() cleans it up afterwards.
/// </summary>
public class AccountControllerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDb _db;
    private readonly AccountController _controller;

    public AccountControllerTests()
    {
        // The in-memory database lives as long as this connection stays open.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _db = new AppDb(new DbContextOptionsBuilder<AppDb>().UseSqlite(_connection).Options);
        _db.Database.EnsureCreated();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["TokenKey"] = TestData.TokenKey })
            .Build();
        _controller = new AccountController(_db, new TokenService(config));
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    /// <summary>Example 2: a [Fact] that walks through a small flow (register, then log in).</summary>
    [Fact]
    public void Register_ThenLoginWithSamePassword_ReturnsToken()
    {
        _controller.Register(new RegisterDto
        {
            FirstName = "Ana",
            LastName = "Pop",
            Email = "ana@example.com",
            Password = "correct-horse"
        });

        var result = _controller.Login(new LoginDto { Email = "ana@example.com", Password = "correct-horse" });

        var ok = Assert.IsType<OkObjectResult>(result);
        var user = Assert.IsType<UserDto>(ok.Value);
        Assert.Equal("ana@example.com", user.Email);
        Assert.False(string.IsNullOrEmpty(user.Token));
        // The password must never be stored in plain text.
        Assert.NotEqual("correct-horse"u8.ToArray(), _db.AppUsers.Single().PasswordHash);
    }

    /// <summary>Example 3: a [Theory] runs the same test once per InlineData row.</summary>
    [Theory]
    [InlineData("ana@example.com", "wrong-password")]   // known email, wrong password
    [InlineData("nobody@example.com", "correct-horse")] // unknown email
    [InlineData("ANA@example.com", "correct-horse")]    // email match is case-sensitive today
    public void Login_WithWrongCredentials_ReturnsBadRequest(string email, string password)
    {
        _controller.Register(new RegisterDto
        {
            FirstName = "Ana",
            LastName = "Pop",
            Email = "ana@example.com",
            Password = "correct-horse"
        });

        var result = _controller.Login(new LoginDto { Email = email, Password = password });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        // Same message for both cases, so the response does not reveal which emails exist.
        Assert.Equal("Invalid username or password.", badRequest.Value);
    }
}
