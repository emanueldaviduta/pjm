using Microsoft.EntityFrameworkCore;
using ProjectManager.Application.Common;
using ProjectManager.Application.DTOs;
using ProjectManager.Application.Services;
using ProjectManager.Domain.Entities;

namespace ProjectManager.Application.Tests;

public class MailSettingsServiceTests : SqliteTestDb
{
    private readonly FakeMailbox _mailbox = new();
    private readonly FakeFileStorage _storage = new();
    private readonly MailSettingsService _service;

    public MailSettingsServiceTests()
    {
        _service = new MailSettingsService(Db, new FakeProtector(), _mailbox, _storage);
    }

    private static SaveMailSettingsRequest Valid(int projectId, string? password = "secret-pw") => new()
    {
        Enabled = true,
        Host = "imap.example.com",
        Port = 993,
        Security = MailSecurity.SslOnConnect,
        Username = "tasks@example.com",
        Password = password,
        Folder = "INBOX",
        AllowedSenders = ["alerts@example.com"],
        ProjectId = projectId,
        PollIntervalSeconds = 60
    };

    private Task<MailSettings> Stored() => Db.MailSettings.AsNoTracking().SingleAsync();

    [Fact]
    public async Task NonAdmin_IsForbidden_ForEveryOperation()
    {
        var bob = await AddUser("Bob");
        var project = await AddProject("P", (bob, OwnerRoleId));

        Assert.Equal(ServiceStatus.Forbidden, (await _service.GetAsync(bob)).Status);
        Assert.Equal(ServiceStatus.Forbidden, (await _service.SaveAsync(bob, Valid(project))).Status);
        Assert.Equal(ServiceStatus.Forbidden, (await _service.TestAsync(bob, Valid(project))).Status);
        Assert.Equal(ServiceStatus.Forbidden, (await _service.GetUsersAsync(bob)).Status);
        Assert.Equal(ServiceStatus.Forbidden, (await _service.GetProjectsAsync(bob)).Status);
        Assert.Empty(Db.MailSettings);
        Assert.Empty(_mailbox.Tests);
    }

    [Fact]
    public async Task Get_BeforeAnySave_ReturnsDefaults()
    {
        var admin = await AddAdmin("Root");

        var settings = (await _service.GetAsync(admin)).Value!;

        Assert.False(settings.Enabled);
        Assert.Equal("INBOX", settings.Folder);
        Assert.Equal(60, settings.PollIntervalSeconds);
        Assert.False(settings.HasPassword);
        Assert.True(settings.AttachmentStorageConfigured);
    }

    [Fact]
    public async Task Save_StoresEncryptedPassword_AndNeverReturnsIt()
    {
        var admin = await AddAdmin("Root");
        var project = await AddProject("P");

        var saved = await _service.SaveAsync(admin, Valid(project));

        Assert.Equal(ServiceStatus.Ok, saved.Status);
        Assert.True(saved.Value!.HasPassword);
        Assert.Equal(["alerts@example.com"], saved.Value.AllowedSenders);
        Assert.Null(typeof(MailSettingsDto).GetProperty("Password"));
        var stored = await Stored();
        Assert.NotEqual("secret-pw", stored.ProtectedPassword);
        Assert.Equal("secret-pw", new FakeProtector().Unprotect(stored.ProtectedPassword!));
    }

    [Fact]
    public async Task Save_WithoutPassword_KeepsTheStoredOne()
    {
        var admin = await AddAdmin("Root");
        var project = await AddProject("P");
        await _service.SaveAsync(admin, Valid(project));
        var before = (await Stored()).ProtectedPassword;

        var request = Valid(project, password: null);
        request.PollIntervalSeconds = 120;
        var result = await _service.SaveAsync(admin, request);

        Assert.Equal(ServiceStatus.Ok, result.Status);
        var stored = await Stored();
        Assert.Equal(before, stored.ProtectedPassword);
        Assert.Equal(120, stored.PollIntervalSeconds);
    }

    public static TheoryData<string, Action<SaveMailSettingsRequest>> Invalids => new()
    {
        { "A host is required.", r => r.Host = " " },
        { "The port must be between 1 and 65535.", r => r.Port = 0 },
        { "The port must be between 1 and 65535.", r => r.Port = 70000 },
        { "A username is required.", r => r.Username = "" },
        { "A password is required.", r => r.Password = null },
        { "At least one allowed sender is required.", r => r.AllowedSenders = [] },
        { "Invalid sender address: not-an-email", r => r.AllowedSenders = ["ok@example.com", "not-an-email"] },
        { "A target project is required.", r => r.ProjectId = null },
        { "The target project does not exist.", r => r.ProjectId = 999 },
        { "The poll interval must be between 30 and 3600 seconds.", r => r.PollIntervalSeconds = 29 },
        { "The poll interval must be between 30 and 3600 seconds.", r => r.PollIntervalSeconds = 3601 },
    };

    [Theory]
    [MemberData(nameof(Invalids))]
    public async Task Save_Enabled_RejectsIncompleteSettings_AndKeepsTheStoredOnes(string message, Action<SaveMailSettingsRequest> breakIt)
    {
        var admin = await AddAdmin("Root");
        var project = await AddProject("P");
        var draft = Valid(project, password: null);
        draft.Enabled = false;
        draft.Host = "old.example.com";
        await _service.SaveAsync(admin, draft);

        // The stored draft has no password, so each request carries one unless the case removes it.
        var request = Valid(project);
        breakIt(request);
        var result = await _service.SaveAsync(admin, request);

        Assert.Equal(ServiceStatus.Invalid, result.Status);
        Assert.Equal(message, result.Message);
        var stored = await Stored();
        Assert.False(stored.Enabled);
        Assert.Equal("old.example.com", stored.Host);
    }

    [Fact]
    public async Task Save_DeletedProject_IsRejected()
    {
        var admin = await AddAdmin("Root");
        var project = await AddProject("P");
        (await Db.Projects.FindAsync(project))!.IsDeleted = true;
        await Db.SaveChangesAsync();

        var result = await _service.SaveAsync(admin, Valid(project));

        Assert.Equal(ServiceStatus.Invalid, result.Status);
        Assert.Equal("The target project does not exist.", result.Message);
    }

    [Fact]
    public async Task Save_DisabledDraft_MayBeIncomplete()
    {
        var admin = await AddAdmin("Root");

        var result = await _service.SaveAsync(admin, new SaveMailSettingsRequest { Enabled = false, Host = "" });

        Assert.Equal(ServiceStatus.Ok, result.Status);
        Assert.False((await Stored()).Enabled);
    }

    [Fact]
    public async Task Save_ChangingTheMailbox_OrReEnabling_ResetsTheCursor()
    {
        var admin = await AddAdmin("Root");
        var project = await AddProject("P");
        await _service.SaveAsync(admin, Valid(project));

        async Task SetCursor()
        {
            var s = await Db.MailSettings.SingleAsync();
            (s.MailboxKey, s.UidValidity, s.LastUid) = ("key", 7, 42);
            await Db.SaveChangesAsync();
            Db.ChangeTracker.Clear();
        }

        await SetCursor();
        await _service.SaveAsync(admin, Valid(project, password: null)); // nothing that matters changed
        Assert.Equal(42, (await Stored()).LastUid);

        var otherFolder = Valid(project, password: null);
        otherFolder.Folder = "Tasks";
        await _service.SaveAsync(admin, otherFolder);
        Assert.Null((await Stored()).LastUid);

        await SetCursor();
        var disabled = Valid(project, password: null);
        disabled.Folder = "Tasks";
        disabled.Enabled = false;
        await _service.SaveAsync(admin, disabled);
        disabled.Enabled = true;
        await _service.SaveAsync(admin, disabled);
        Assert.Null((await Stored()).LastUid);
    }

    [Fact]
    public async Task Test_UsesTheStoredPasswordWhenNoneIsSent()
    {
        var admin = await AddAdmin("Root");
        var project = await AddProject("P");
        await _service.SaveAsync(admin, Valid(project));

        var result = await _service.TestAsync(admin, Valid(project, password: null));

        Assert.True(result.Value!.Success);
        Assert.Equal("secret-pw", Assert.Single(_mailbox.Tests).Password);
    }

    [Fact]
    public async Task Test_Failure_IsReported_AndChangesNothing()
    {
        var admin = await AddAdmin("Root");
        var project = await AddProject("P");
        await _service.SaveAsync(admin, Valid(project));
        var before = await Stored();
        _mailbox.TestResult = new(false, "Authentication failed.");

        var request = Valid(project, password: "wrong");
        request.Host = "other.example.com";
        var result = await _service.TestAsync(admin, request);

        Assert.Equal(ServiceStatus.Ok, result.Status);
        Assert.False(result.Value!.Success);
        Assert.Equal("Authentication failed.", result.Value.Message);
        var after = await Stored();
        Assert.Equal(before.Host, after.Host);
        Assert.Equal(before.ProtectedPassword, after.ProtectedPassword);
        Assert.Empty(Db.TaskItems);
    }

    [Fact]
    public async Task Users_IncludeTheAdminFlag_AndProjects_ExcludeDeletedOnes()
    {
        var admin = await AddAdmin("Root");
        await AddUser("Bob");
        var keep = await AddProject("Keep");
        var drop = await AddProject("Drop");
        (await Db.Projects.FindAsync(drop))!.IsDeleted = true;
        await Db.SaveChangesAsync();

        var users = (await _service.GetUsersAsync(admin)).Value!;
        var projects = (await _service.GetProjectsAsync(admin)).Value!;

        Assert.Equal([("Bob", false), ("Root", true)], users.Select(u => (u.FirstName, u.IsAdmin)));
        Assert.Equal([keep], projects.Select(p => p.Id));
    }

    [Fact]
    public async Task StorageNotConfigured_IsReported()
    {
        var admin = await AddAdmin("Root");
        _storage.IsConfigured = false;

        Assert.False((await _service.GetAsync(admin)).Value!.AttachmentStorageConfigured);
    }
}
