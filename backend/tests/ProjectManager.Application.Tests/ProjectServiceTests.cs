using Microsoft.EntityFrameworkCore;
using ProjectManager.Application.Common;
using ProjectManager.Application.DTOs;
using ProjectManager.Application.Services;

namespace ProjectManager.Application.Tests;

public class ProjectServiceTests : SqliteTestDb
{
    private readonly ProjectService _service;

    public ProjectServiceTests()
    {
        _service = new ProjectService(Db);
    }

    private static CreateProjectRequest NewProject(string name) => new() { Name = name, Description = "d" };

    [Fact]
    public async Task Create_StoresProject_AndSetsCreatedAt()
    {
        var ana = await AddUser("Ana");
        var before = DateTime.UtcNow;

        var created = await _service.CreateAsync(ana, NewProject("Alpha"));

        Assert.NotEqual(0, created.Id);
        Assert.True(created.CreatedAt >= before);
        Assert.Single(Db.Projects);
    }

    [Fact]
    public async Task Create_MakesTheCreatorOwner()
    {
        var ana = await AddUser("Ana");

        var created = await _service.CreateAsync(ana, NewProject("Alpha"));

        Assert.Equal(["Owner"], created.Roles);
        Assert.Equal([OwnerRoleId], await RoleIds(created.Id, ana));
        var listed = Assert.Single(await _service.GetAllAsync(ana));
        Assert.Equal(["Owner"], listed.Roles);
    }

    [Fact]
    public async Task Create_IsNotVisibleToOtherUsers()
    {
        var ana = await AddUser("Ana");
        var bob = await AddUser("Bob");

        await _service.CreateAsync(ana, NewProject("Alpha"));

        Assert.Empty(await _service.GetAllAsync(bob));
    }

    [Fact]
    public async Task GetAll_ReturnsOnlyTheCallersRoles()
    {
        var ana = await AddUser("Ana");
        var bob = await AddUser("Bob");
        await AddProject("P", (ana, OwnerRoleId), (ana, MemberRoleId), (bob, MemberRoleId));

        Assert.Equal(["Owner", "Member"], Assert.Single(await _service.GetAllAsync(ana)).Roles);
        Assert.Equal(["Member"], Assert.Single(await _service.GetAllAsync(bob)).Roles);
    }

    [Fact]
    public async Task GetAll_ExcludesSoftDeletedProjects()
    {
        var ana = await AddUser("Ana");
        var keep = await _service.CreateAsync(ana, NewProject("Keep"));
        var drop = await _service.CreateAsync(ana, NewProject("Drop"));
        await _service.DeleteAsync(ana, drop.Id);

        var all = await _service.GetAllAsync(ana);

        var only = Assert.Single(all);
        Assert.Equal(keep.Id, only.Id);
    }

    [Fact]
    public async Task Delete_ByOwner_SoftDeletes_AndReturnsRemainingProjects()
    {
        var ana = await AddUser("Ana");
        var a = await _service.CreateAsync(ana, NewProject("A"));
        var b = await _service.CreateAsync(ana, NewProject("B"));

        var result = await _service.DeleteAsync(ana, a.Id);

        Assert.Equal(ServiceStatus.Ok, result.Status);
        Assert.Equal(b.Id, Assert.Single(result.Value!).Id);
        var stored = await Db.Projects.SingleAsync(p => p.Id == a.Id);
        Assert.True(stored.IsDeleted);
    }

    [Fact]
    public async Task Delete_ByMemberWithoutOwner_IsForbidden_AndChangesNothing()
    {
        var ana = await AddUser("Ana");
        var bob = await AddUser("Bob");
        var project = await AddProject("P", (ana, OwnerRoleId), (bob, MemberRoleId));

        var result = await _service.DeleteAsync(bob, project);

        Assert.Equal(ServiceStatus.Forbidden, result.Status);
        Assert.False((await Db.Projects.AsNoTracking().SingleAsync(p => p.Id == project)).IsDeleted);
    }

    [Fact]
    public async Task Delete_ByNonMember_IsNotFound_AndChangesNothing()
    {
        var ana = await AddUser("Ana");
        var bob = await AddUser("Bob");
        var project = await AddProject("P", (ana, OwnerRoleId));

        var result = await _service.DeleteAsync(bob, project);

        Assert.Equal(ServiceStatus.NotFound, result.Status);
        Assert.False((await Db.Projects.AsNoTracking().SingleAsync(p => p.Id == project)).IsDeleted);
    }

    [Fact]
    public async Task Delete_UnknownProject_ReturnsNotFound()
    {
        var ana = await AddUser("Ana");

        var result = await _service.DeleteAsync(ana, 999);

        Assert.Equal(ServiceStatus.NotFound, result.Status);
        Assert.Null(result.Value);
    }
}
