using ProjectManager.Application.Common;
using ProjectManager.Application.Services;

namespace ProjectManager.Application.Tests;

public class ProjectMembershipServiceTests : SqliteTestDb
{
    private readonly ProjectMembershipService _service;

    public ProjectMembershipServiceTests()
    {
        _service = new ProjectMembershipService(Db);
    }

    // --- Add -----------------------------------------------------------------------------------

    [Fact]
    public async Task Owner_AddsMembers_AndASecondRole()
    {
        var ana = await AddUser("Ana");
        var bob = await AddUser("Bob");
        var cid = await AddUser("Cid");
        var project = await AddProject("P", (ana, OwnerRoleId));

        Assert.Equal(ServiceStatus.Ok, (await _service.AddRoleAsync(ana, project, bob, MemberRoleId)).Status);
        Assert.Equal(ServiceStatus.Ok, (await _service.AddRoleAsync(ana, project, cid, MemberRoleId)).Status);
        var both = await _service.AddRoleAsync(ana, project, bob, OwnerRoleId);

        Assert.Equal(ServiceStatus.Ok, both.Status);
        Assert.Equal(["Owner", "Member"], both.Value!.Roles.OrderBy(r => r.Id).Select(r => r.Name));
        Assert.Equal([MemberRoleId], await RoleIds(project, cid));
    }

    [Fact]
    public async Task Member_CannotAdd()
    {
        var ana = await AddUser("Ana");
        var bob = await AddUser("Bob");
        var cid = await AddUser("Cid");
        var project = await AddProject("P", (ana, OwnerRoleId), (bob, MemberRoleId));

        var result = await _service.AddRoleAsync(bob, project, cid, MemberRoleId);

        Assert.Equal(ServiceStatus.Forbidden, result.Status);
        Assert.Empty(await RoleIds(project, cid));
    }

    [Fact]
    public async Task NonMember_CannotAdd_AndGetsNotFound()
    {
        var ana = await AddUser("Ana");
        var bob = await AddUser("Bob");
        var project = await AddProject("P", (ana, OwnerRoleId));

        var result = await _service.AddRoleAsync(bob, project, bob, OwnerRoleId);

        Assert.Equal(ServiceStatus.NotFound, result.Status);
        Assert.Empty(await RoleIds(project, bob));
    }

    [Fact]
    public async Task Add_Duplicate_IsConflict()
    {
        var ana = await AddUser("Ana");
        var bob = await AddUser("Bob");
        var project = await AddProject("P", (ana, OwnerRoleId), (bob, MemberRoleId));

        var result = await _service.AddRoleAsync(ana, project, bob, MemberRoleId);

        Assert.Equal(ServiceStatus.Conflict, result.Status);
        Assert.Equal([MemberRoleId], await RoleIds(project, bob));
    }

    [Fact]
    public async Task Add_UnknownUserOrRole_IsNotFound()
    {
        var ana = await AddUser("Ana");
        var bob = await AddUser("Bob");
        var project = await AddProject("P", (ana, OwnerRoleId));

        Assert.Equal(ServiceStatus.NotFound, (await _service.AddRoleAsync(ana, project, 999, MemberRoleId)).Status);
        Assert.Equal(ServiceStatus.NotFound, (await _service.AddRoleAsync(ana, project, bob, 999)).Status);
        Assert.Empty(await RoleIds(project, bob));
    }

    // --- List ----------------------------------------------------------------------------------

    [Fact]
    public async Task Member_ListsMembers_GroupedPerUser()
    {
        var ana = await AddUser("Ana");
        var bob = await AddUser("Bob");
        var project = await AddProject("P", (ana, OwnerRoleId), (ana, MemberRoleId), (bob, MemberRoleId));

        var result = await _service.GetMembersAsync(bob, project);

        Assert.Equal(ServiceStatus.Ok, result.Status);
        Assert.Collection(result.Value!,
            a => Assert.Equal((ana, 2), (a.UserId, a.Roles.Count)),
            b => Assert.Equal((bob, 1), (b.UserId, b.Roles.Count)));
    }

    [Fact]
    public async Task NonMember_ListGetsSameNotFoundAsUnknownProject()
    {
        var ana = await AddUser("Ana");
        var bob = await AddUser("Bob");
        var project = await AddProject("P", (ana, OwnerRoleId));

        var existing = await _service.GetMembersAsync(bob, project);
        var unknown = await _service.GetMembersAsync(bob, 999);

        Assert.Equal(ServiceStatus.NotFound, existing.Status);
        Assert.Equal(unknown, existing);
    }

    // --- Remove --------------------------------------------------------------------------------

    [Fact]
    public async Task Member_CannotRemoveRoleOrMember()
    {
        var ana = await AddUser("Ana");
        var bob = await AddUser("Bob");
        var cid = await AddUser("Cid");
        var project = await AddProject("P", (ana, OwnerRoleId), (bob, MemberRoleId), (cid, MemberRoleId));

        Assert.Equal(ServiceStatus.Forbidden, (await _service.RemoveRoleAsync(bob, project, cid, MemberRoleId)).Status);
        Assert.Equal(ServiceStatus.Forbidden, (await _service.RemoveMemberAsync(bob, project, cid)).Status);
        Assert.Equal([MemberRoleId], await RoleIds(project, cid));
    }

    [Fact]
    public async Task Owner_RemovesMember_OtherProjectsUnaffected()
    {
        var ana = await AddUser("Ana");
        var bob = await AddUser("Bob");
        var project = await AddProject("P", (ana, OwnerRoleId), (bob, MemberRoleId));
        var other = await AddProject("Q", (bob, OwnerRoleId));

        var result = await _service.RemoveMemberAsync(ana, project, bob);

        Assert.Equal(ServiceStatus.Ok, result.Status);
        Assert.Empty(await RoleIds(project, bob));
        Assert.Equal([OwnerRoleId], await RoleIds(other, bob));
        Assert.NotNull(await Db.AppUsers.FindAsync(bob));
    }

    [Fact]
    public async Task Remove_SomethingThatIsNotThere_IsNotFound()
    {
        var ana = await AddUser("Ana");
        var bob = await AddUser("Bob");
        var project = await AddProject("P", (ana, OwnerRoleId));

        Assert.Equal(ServiceStatus.NotFound, (await _service.RemoveRoleAsync(ana, project, bob, MemberRoleId)).Status);
        Assert.Equal(ServiceStatus.NotFound, (await _service.RemoveMemberAsync(ana, project, bob)).Status);
    }

    [Fact]
    public async Task RemovingTheLastOwnerRole_IsRejected()
    {
        var ana = await AddUser("Ana");
        var project = await AddProject("P", (ana, OwnerRoleId), (ana, MemberRoleId));

        var result = await _service.RemoveRoleAsync(ana, project, ana, OwnerRoleId);

        Assert.Equal(ServiceStatus.Invalid, result.Status);
        Assert.Equal("A project must keep at least one owner.", result.Message);
        Assert.Equal([OwnerRoleId, MemberRoleId], await RoleIds(project, ana));
    }

    [Fact]
    public async Task RemovingTheLastOwnerAsMember_IsRejected()
    {
        var ana = await AddUser("Ana");
        var bob = await AddUser("Bob");
        var project = await AddProject("P", (ana, OwnerRoleId), (ana, MemberRoleId), (bob, MemberRoleId));

        var result = await _service.RemoveMemberAsync(ana, project, ana);

        Assert.Equal(ServiceStatus.Invalid, result.Status);
        Assert.Equal([OwnerRoleId, MemberRoleId], await RoleIds(project, ana));
    }

    [Fact]
    public async Task RemovingAnOwner_WorksWhileAnotherOwnerRemains()
    {
        var ana = await AddUser("Ana");
        var bob = await AddUser("Bob");
        var project = await AddProject("P", (ana, OwnerRoleId), (bob, OwnerRoleId), (bob, MemberRoleId));

        var result = await _service.RemoveRoleAsync(ana, project, bob, OwnerRoleId);

        Assert.Equal(ServiceStatus.Ok, result.Status);
        Assert.Equal([MemberRoleId], await RoleIds(project, bob));
        Assert.Equal([OwnerRoleId], await RoleIds(project, ana));
    }
}
