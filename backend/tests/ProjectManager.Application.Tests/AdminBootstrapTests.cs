using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ProjectManager.Application.Common;
using ProjectManager.Application.DTOs;
using ProjectManager.Application.Services;
using ProjectManager.Infrastructure.Services;

namespace ProjectManager.Application.Tests;

public class AdminBootstrapTests : SqliteTestDb
{
    private Task<bool> IsAdmin(int userId) => Db.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.RoleId == AdminRoleId);

    private AccountService Accounts(string? adminEmail)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["TokenKey"] = TestData.TokenKey })
            .Build();
        return new AccountService(Db, new TokenService(config), new AdminOptions { Email = adminEmail });
    }

    private static RegisterDto Registration(string email) =>
        new() { FirstName = "Boss", LastName = "Test", Email = email, Password = "correct-horse" };

    [Fact]
    public async Task Startup_PromotesTheExistingConfiguredUser()
    {
        var boss = await AddUser("Boss");
        var other = await AddUser("Other");

        await Db.EnsureConfiguredAdminAsync(new AdminOptions { Email = "boss@example.com" });

        Assert.True(await IsAdmin(boss));
        Assert.False(await IsAdmin(other));
    }

    [Fact]
    public async Task Startup_IgnoresCase()
    {
        var boss = await AddUser("Boss");

        await Db.EnsureConfiguredAdminAsync(new AdminOptions { Email = "  BOSS@Example.com " });

        Assert.True(await IsAdmin(boss));
    }

    [Fact]
    public async Task Startup_RunTwice_DoesNotDuplicateTheRole()
    {
        var boss = await AddUser("Boss");
        var options = new AdminOptions { Email = "boss@example.com" };

        await Db.EnsureConfiguredAdminAsync(options);
        await Db.EnsureConfiguredAdminAsync(options);

        Assert.Equal(1, await Db.UserRoles.CountAsync(ur => ur.UserId == boss && ur.RoleId == AdminRoleId));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task NoConfiguredEmail_GrantsNobody(string? email)
    {
        await AddUser("Boss");
        await Db.EnsureConfiguredAdminAsync(new AdminOptions { Email = email });
        await Accounts(email).RegisterAsync(Registration("someone@example.com"));

        Assert.Empty(Db.UserRoles);
    }

    [Fact]
    public async Task Registration_PromotesTheConfiguredEmail_IgnoringCase()
    {
        var accounts = Accounts("Boss@Example.com");

        await accounts.RegisterAsync(Registration("boss@example.com"));
        await accounts.RegisterAsync(Registration("worker@example.com"));

        var boss = await Db.AppUsers.SingleAsync(u => u.Email == "boss@example.com");
        var worker = await Db.AppUsers.SingleAsync(u => u.Email == "worker@example.com");
        Assert.True(await IsAdmin(boss.Id));
        Assert.False(await IsAdmin(worker.Id));
    }
}
