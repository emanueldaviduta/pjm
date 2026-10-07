using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProjectManager.Application.Common;
using ProjectManager.Application.Services;
using ProjectManager.Domain.Entities;
using ProjectManager.Infrastructure.Persistence;

namespace ProjectManager.Application.Tests;

public class ProjectServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDb _db;
    private readonly ProjectService _service;

    public ProjectServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _db = new AppDb(new DbContextOptionsBuilder<AppDb>().UseSqlite(_connection).Options);
        _db.Database.EnsureCreated();
        _service = new ProjectService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private static Project NewProject(string name) => new() { Name = name, Description = "d" };

    [Fact]
    public async Task Create_StoresProject_AndSetsCreatedAt()
    {
        var before = DateTime.UtcNow;

        var created = await _service.CreateAsync(NewProject("Alpha"));

        Assert.NotEqual(0, created.Id);
        Assert.True(created.CreatedAt >= before);
        Assert.Single(_db.Projects);
    }

    [Fact]
    public async Task GetAll_ExcludesSoftDeletedProjects()
    {
        var keep = await _service.CreateAsync(NewProject("Keep"));
        var drop = await _service.CreateAsync(NewProject("Drop"));
        await _service.DeleteAsync(drop.Id);

        var all = await _service.GetAllAsync();

        var only = Assert.Single(all);
        Assert.Equal(keep.Id, only.Id);
    }

    [Fact]
    public async Task Delete_SoftDeletes_AndReturnsEveryProject()
    {
        var a = await _service.CreateAsync(NewProject("A"));
        await _service.CreateAsync(NewProject("B"));

        var result = await _service.DeleteAsync(a.Id);

        Assert.Equal(ServiceStatus.Ok, result.Status);
        // The endpoint has always returned the full list here, deleted projects included.
        Assert.Equal(2, result.Value!.Count);
        var stored = await _db.Projects.SingleAsync(p => p.Id == a.Id);
        Assert.True(stored.IsDeleted);
    }

    [Fact]
    public async Task Delete_UnknownProject_ReturnsNotFound()
    {
        var result = await _service.DeleteAsync(999);

        Assert.Equal(ServiceStatus.NotFound, result.Status);
        Assert.Null(result.Value);
    }
}
