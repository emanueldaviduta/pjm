using Microsoft.EntityFrameworkCore;
using ProjectManager.Application.Common;
using ProjectManager.Application.Services;

namespace ProjectManager.Application.Tests;

public class RoleServiceTests : SqliteTestDb
{
    private readonly RoleService _service;

    public RoleServiceTests()
    {
        _service = new RoleService(Db);
    }

    private Task<List<int>> GlobalRoleIds(int userId) =>
        Db.UserRoles.AsNoTracking().Where(ur => ur.UserId == userId).OrderBy(ur => ur.RoleId).Select(ur => ur.RoleId).ToListAsync();

    [Fact]
    public async Task Catalog_ContainsOwnerMemberAndAdmin()
    {
        Assert.Equal(["Owner", "Member", "Admin"], (await _service.GetAllAsync()).Select(r => r.Name));
    }

    [Fact]
    public async Task Admin_AssignsListsAndRemovesRoles()
    {
        var admin = await AddAdmin("Ana");
        var bob = await AddUser("Bob");

        Assert.Equal(ServiceStatus.Ok, (await _service.AssignToUserAsync(admin, bob, MemberRoleId)).Status);
        Assert.Equal(ServiceStatus.Ok, (await _service.AssignToUserAsync(admin, bob, OwnerRoleId)).Status);
        var listed = await _service.GetUserRolesAsync(admin, bob);
        Assert.Equal(["Owner", "Member"], listed.Value!.Select(r => r.Name));

        Assert.Equal(ServiceStatus.Ok, (await _service.RemoveFromUserAsync(admin, bob, OwnerRoleId)).Status);
        Assert.Equal([MemberRoleId], await GlobalRoleIds(bob));
    }

    [Fact]
    public async Task Admin_GrantsAdmin()
    {
        var admin = await AddAdmin("Ana");
        var bob = await AddUser("Bob");

        var result = await _service.AssignToUserAsync(admin, bob, AdminRoleId);

        Assert.Equal(ServiceStatus.Ok, result.Status);
        Assert.Equal([AdminRoleId], await GlobalRoleIds(bob));
    }

    [Fact]
    public async Task NonAdmin_IsForbidden_ForEveryOperation_AndChangesNothing()
    {
        await AddAdmin("Ana");
        var bob = await AddUser("Bob");
        var cid = await AddUser("Cid");
        Db.UserRoles.Add(new() { UserId = cid, RoleId = MemberRoleId });
        await Db.SaveChangesAsync();

        Assert.Equal(ServiceStatus.Forbidden, (await _service.AssignToUserAsync(bob, bob, AdminRoleId)).Status);
        Assert.Equal(ServiceStatus.Forbidden, (await _service.GetUserRolesAsync(bob, cid)).Status);
        Assert.Equal(ServiceStatus.Forbidden, (await _service.RemoveFromUserAsync(bob, cid, MemberRoleId)).Status);

        Assert.Empty(await GlobalRoleIds(bob));
        Assert.Equal([MemberRoleId], await GlobalRoleIds(cid));
    }

    [Fact]
    public async Task Assign_Duplicate_IsConflict()
    {
        var admin = await AddAdmin("Ana");
        var bob = await AddUser("Bob");
        await _service.AssignToUserAsync(admin, bob, MemberRoleId);

        var result = await _service.AssignToUserAsync(admin, bob, MemberRoleId);

        Assert.Equal(ServiceStatus.Conflict, result.Status);
        Assert.Equal([MemberRoleId], await GlobalRoleIds(bob));
    }

    [Fact]
    public async Task UnknownUserOrRole_IsNotFound()
    {
        var admin = await AddAdmin("Ana");
        var bob = await AddUser("Bob");

        Assert.Equal(ServiceStatus.NotFound, (await _service.AssignToUserAsync(admin, 999, MemberRoleId)).Status);
        Assert.Equal(ServiceStatus.NotFound, (await _service.AssignToUserAsync(admin, bob, 999)).Status);
        Assert.Equal(ServiceStatus.NotFound, (await _service.GetUserRolesAsync(admin, 999)).Status);
        Assert.Equal(ServiceStatus.NotFound, (await _service.RemoveFromUserAsync(admin, bob, MemberRoleId)).Status);
    }

    [Fact]
    public async Task RemovingTheLastAdmin_IsRejected()
    {
        var admin = await AddAdmin("Ana");

        var result = await _service.RemoveFromUserAsync(admin, admin, AdminRoleId);

        Assert.Equal(ServiceStatus.Invalid, result.Status);
        Assert.Equal("At least one admin must remain.", result.Message);
        Assert.Equal([AdminRoleId], await GlobalRoleIds(admin));
    }

    [Fact]
    public async Task RemovingAnAdmin_WorksWhileAnotherAdminRemains()
    {
        var ana = await AddAdmin("Ana");
        var bob = await AddAdmin("Bob");

        var result = await _service.RemoveFromUserAsync(ana, bob, AdminRoleId);

        Assert.Equal(ServiceStatus.Ok, result.Status);
        Assert.Empty(await GlobalRoleIds(bob));
    }
}
