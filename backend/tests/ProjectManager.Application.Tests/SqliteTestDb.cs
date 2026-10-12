using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProjectManager.Domain.Entities;
using ProjectManager.Infrastructure.Persistence;

namespace ProjectManager.Application.Tests;

/// <summary>
/// An empty in-memory SQLite database per test (xUnit creates one instance per test), with the seeded
/// roles, plus helpers to set up users, projects and memberships directly.
/// </summary>
public abstract class SqliteTestDb : IDisposable
{
    protected const int OwnerRoleId = 1;
    protected const int MemberRoleId = 2;
    protected const int AdminRoleId = 3;

    private readonly SqliteConnection _connection;
    protected readonly AppDb Db;

    protected SqliteTestDb()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        Db = new AppDb(new DbContextOptionsBuilder<AppDb>().UseSqlite(_connection).Options);
        Db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    protected async Task<int> AddUser(string firstName)
    {
        var user = new AppUser
        {
            FirstName = firstName,
            LastName = "Test",
            Email = $"{firstName.ToLowerInvariant()}@example.com",
            PasswordHash = [1],
            PasswordSalt = [1]
        };
        Db.AppUsers.Add(user);
        await Db.SaveChangesAsync();
        return user.Id;
    }

    /// <summary>Adds a user who holds the global Admin role.</summary>
    protected async Task<int> AddAdmin(string firstName)
    {
        var id = await AddUser(firstName);
        Db.UserRoles.Add(new UserRole { UserId = id, RoleId = AdminRoleId });
        await Db.SaveChangesAsync();
        return id;
    }

    /// <summary>Adds a project and gives each listed user the listed role in it.</summary>
    protected async Task<int> AddProject(string name, params (int UserId, int RoleId)[] members)
    {
        var project = new Project { Name = name, Description = "d" };
        Db.Projects.Add(project);
        await Db.SaveChangesAsync();
        foreach (var (userId, roleId) in members)
        {
            await Grant(project.Id, userId, roleId);
        }
        return project.Id;
    }

    protected async Task Grant(int projectId, int userId, int roleId)
    {
        Db.ProjectUserRoles.Add(new ProjectUserRole { ProjectId = projectId, UserId = userId, RoleId = roleId });
        await Db.SaveChangesAsync();
    }

    /// <summary>Role ids a user holds in a project, read fresh from the database.</summary>
    protected Task<List<int>> RoleIds(int projectId, int userId) =>
        Db.ProjectUserRoles.AsNoTracking()
            .Where(r => r.ProjectId == projectId && r.UserId == userId)
            .OrderBy(r => r.RoleId)
            .Select(r => r.RoleId)
            .ToListAsync();
}
