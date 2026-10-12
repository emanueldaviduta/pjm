using Microsoft.EntityFrameworkCore;
using ProjectManager.Application.Common;

namespace ProjectManager.Application.Tests;

public class ProjectAccessTests : SqliteTestDb
{
    [Fact]
    public async Task VisibleProjects_IncludesProjectsTheUserBelongsTo()
    {
        var ana = await AddUser("Ana");
        var project = await AddProject("P", (ana, MemberRoleId));

        var visible = await Db.VisibleProjects(ana).Select(p => p.Id).ToListAsync();

        Assert.Equal([project], visible);
    }

    [Fact]
    public async Task VisibleProjects_ExcludesProjectsOfOtherUsers()
    {
        var ana = await AddUser("Ana");
        var bob = await AddUser("Bob");
        await AddProject("P", (ana, OwnerRoleId));

        Assert.Empty(await Db.VisibleProjects(bob).ToListAsync());
    }

    [Fact]
    public async Task VisibleProjects_ExcludesDeletedProjects()
    {
        var ana = await AddUser("Ana");
        var project = await AddProject("P", (ana, OwnerRoleId));
        (await Db.Projects.FindAsync(project))!.IsDeleted = true;
        await Db.SaveChangesAsync();

        Assert.Empty(await Db.VisibleProjects(ana).ToListAsync());
        Assert.Empty(await Db.RolesInProjectAsync(project, ana));
    }

    [Fact]
    public async Task ProjectWithoutMembers_IsVisibleToNobody()
    {
        var ana = await AddUser("Ana");
        await AddProject("Orphan");

        Assert.Empty(await Db.VisibleProjects(ana).ToListAsync());
    }

    [Fact]
    public async Task RolesInProject_ReturnsOnlyTheCallersRoles()
    {
        var ana = await AddUser("Ana");
        var bob = await AddUser("Bob");
        var project = await AddProject("P", (ana, MemberRoleId), (bob, OwnerRoleId));

        Assert.Equal(new HashSet<string> { "Member" }, await Db.RolesInProjectAsync(project, ana));
        Assert.Equal(new HashSet<string> { "Owner" }, await Db.RolesInProjectAsync(project, bob));
    }

    [Fact]
    public async Task RolesInProject_UnknownProjectOrNonMember_IsEmpty()
    {
        var ana = await AddUser("Ana");
        var bob = await AddUser("Bob");
        var project = await AddProject("P", (ana, OwnerRoleId));

        Assert.Empty(await Db.RolesInProjectAsync(project, bob));
        Assert.Empty(await Db.RolesInProjectAsync(999, ana));
    }
}
