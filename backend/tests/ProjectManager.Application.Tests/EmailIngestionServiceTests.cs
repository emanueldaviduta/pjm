using Microsoft.EntityFrameworkCore;
using ProjectManager.Application.Interfaces;
using ProjectManager.Application.Services;
using ProjectManager.Domain.Entities;

namespace ProjectManager.Application.Tests;

public class EmailIngestionServiceTests : SqliteTestDb
{
    private const string Allowed = "alerts@example.com";

    private readonly FakeMailbox _mailbox = new();
    private readonly FakeFileStorage _storage = new();
    private int _member;
    private int _project;

    // A new instance per call, as the worker does with a fresh scope each poll (and as after a restart).
    private EmailIngestionService Service() => new(Db, _mailbox, _storage, new FakeProtector());

    private async Task<MailPollOutcome> Poll()
    {
        var outcome = await Service().PollOnceAsync();
        Db.ChangeTracker.Clear();
        return outcome;
    }

    /// <summary>Enabled settings on a project with one member, and the first (baseline) poll already done.</summary>
    private async Task Enable(bool baseline = true)
    {
        _member = await AddUser("Mia");
        _project = await AddProject("Ops", (_member, OwnerRoleId));
        Db.MailSettings.Add(new MailSettings
        {
            Enabled = true,
            Host = "imap.example.com",
            Username = "tasks@example.com",
            ProtectedPassword = new FakeProtector().Protect("pw"),
            AllowedSenders = [Allowed],
            ProjectId = _project
        });
        await Db.SaveChangesAsync();
        if (baseline)
        {
            await Poll();
        }
    }

    private Task<MailSettings> Settings() => Db.MailSettings.AsNoTracking().SingleAsync();

    private Task<List<TaskItem>> Tasks() => Db.TaskItems.AsNoTracking().Include(t => t.Attachments).OrderBy(t => t.Id).ToListAsync();

    private static MailboxAttachment File(string name, int bytes = 3) =>
        new(name, "application/pdf", bytes, new byte[bytes], null);

    private static MailboxAttachment NotDownloaded(string name, string reason) =>
        new(name, "application/octet-stream", 25 * 1024 * 1024, null, reason);

    [Fact]
    public async Task AllowedSender_BecomesOneTask_WithTheMessageContent()
    {
        await Enable();
        _mailbox.Add(Allowed, "Server down", "Disk full on db-1");

        var outcome = await Poll();

        Assert.Equal(1, outcome.Created);
        var task = Assert.Single(await Tasks());
        Assert.Equal(_project, task.ProjectId);
        Assert.Equal("Server down", task.Title);
        Assert.Contains($"From: {Allowed}", task.Description);
        Assert.Contains("Received: 2026-10-12 09:30 UTC", task.Description);
        Assert.Contains("Disk full on db-1", task.Description);
        Assert.Equal((Status.Created, Priority.Medium), (task.Status, task.Priority));
        Assert.Null(task.DueDate);
        Assert.Null(task.AssignedId);
        var inbound = await Db.InboundEmails.SingleAsync();
        Assert.Equal((InboundEmailOutcome.Created, task.Id), (inbound.Outcome, inbound.TaskItemId));
    }

    [Fact]
    public async Task SenderMatch_IgnoresCase()
    {
        await Enable();
        _mailbox.Add("Alerts@Example.COM");

        await Poll();

        Assert.Single(await Tasks());
    }

    [Fact]
    public async Task Subject_EmptyBecomesNoSubject_LongIsTrimmedTo200()
    {
        await Enable();
        _mailbox.Add(Allowed, subject: "   ");
        _mailbox.Add(Allowed, subject: new string('x', 250));

        await Poll();

        var tasks = await Tasks();
        Assert.Equal("(no subject)", tasks[0].Title);
        Assert.Equal(200, tasks[1].Title.Length);
    }

    [Fact]
    public async Task Body_IsCappedAt10000Characters()
    {
        await Enable();
        _mailbox.Add(Allowed, body: new string('b', 12_000));

        await Poll();

        Assert.Equal(10_000, (await Tasks())[0].Description.Count(c => c == 'b'));
    }

    [Fact]
    public async Task UnknownSender_IsSkipped_AndRecorded()
    {
        await Enable();
        _mailbox.Add("stranger@example.com");

        var outcome = await Poll();

        Assert.Equal(0, outcome.Created);
        Assert.Empty(await Tasks());
        Assert.Equal(InboundEmailOutcome.Skipped, (await Db.InboundEmails.SingleAsync()).Outcome);
        Assert.Equal(1, (await Settings()).LastUid);
    }

    [Fact]
    public async Task RepeatedPolls_AndARestart_CreateNoDuplicates()
    {
        await Enable();
        _mailbox.Add(Allowed, messageId: "<same@test>");
        await Poll();

        await Poll();
        await Poll(); // each Poll() uses a new service instance, as after a restart
        // The same message delivered again (e.g. copied back into the folder) is still processed once.
        _mailbox.Add(Allowed, messageId: "<same@test>");
        await Poll();

        Assert.Single(await Tasks());
        Assert.Equal(2, (await Settings()).LastUid);
    }

    [Fact]
    public async Task MailAlreadyInTheFolderWhenEnabled_IsNotImported()
    {
        await Enable(baseline: false);
        for (var i = 0; i < 5; i++) _mailbox.Add(Allowed);

        await Poll();
        Assert.Empty(await Tasks());
        Assert.Equal(5, (await Settings()).LastUid);

        _mailbox.Add(Allowed, "New one");
        await Poll();
        Assert.Equal("New one", Assert.Single(await Tasks()).Title);
    }

    [Fact]
    public async Task UidValidityReset_TakesANewBaseline_WithoutImporting()
    {
        await Enable();
        _mailbox.Add(Allowed);
        _mailbox.UidValidity = 2;

        await Poll();

        Assert.Empty(await Tasks());
        Assert.Equal(2, (await Settings()).UidValidity);
    }

    [Fact]
    public async Task DeletedProject_HoldsTheCursor_AndMailIsImportedOnceFixed()
    {
        await Enable();
        var project = await Db.Projects.FindAsync(_project);
        project!.IsDeleted = true;
        await Db.SaveChangesAsync();
        _mailbox.Add(Allowed, "While broken");

        var failed = await Poll();

        Assert.Equal("The target project is missing or has been deleted.", failed.Error);
        Assert.Empty(await Tasks());
        var settings = await Settings();
        Assert.Equal(0, settings.LastUid);
        Assert.NotNull(settings.LastPollAt);
        Assert.Equal(failed.Error, settings.LastPollError);

        (await Db.Projects.FindAsync(_project))!.IsDeleted = false;
        await Db.SaveChangesAsync();
        var recovered = await Poll();

        Assert.Null(recovered.Error);
        Assert.Equal("While broken", Assert.Single(await Tasks()).Title);
        Assert.Null((await Settings()).LastPollError);
    }

    [Fact]
    public async Task ClientError_IsRecorded_AndTheNextPollRecovers()
    {
        await Enable();
        _mailbox.Add(Allowed);
        _mailbox.FailNextFetch = new IOException("Connection refused");

        var failed = await Poll();

        Assert.Equal("Connection refused", failed.Error);
        Assert.Equal("Connection refused", (await Settings()).LastPollError);
        Assert.Empty(await Tasks());

        var recovered = await Poll();
        Assert.Null(recovered.Error);
        Assert.Single(await Tasks());
        Assert.Null((await Settings()).LastPollError);
    }

    [Fact]
    public async Task Disabled_DoesNotConnect()
    {
        await Enable();
        var settings = await Db.MailSettings.SingleAsync();
        settings.Enabled = false;
        await Db.SaveChangesAsync();
        _mailbox.Add(Allowed);
        var callsBefore = _mailbox.FetchCalls;

        var outcome = await Poll();

        Assert.False(outcome.Enabled);
        Assert.Equal(callsBefore, _mailbox.FetchCalls);
        Assert.Empty(await Tasks());
    }

    [Fact]
    public async Task CreatedTasks_AreVisibleToProjectMembersOnly()
    {
        await Enable();
        var outsider = await AddUser("Out");
        _mailbox.Add(Allowed, "Visible");
        await Poll();

        var tasks = new TaskService(Db);
        Assert.Equal("Visible", Assert.Single(await tasks.GetAllAsync(_member)).Title);
        Assert.Empty(await tasks.GetAllAsync(outsider));
    }

    // --- Attachments --------------------------------------------------------------------------

    [Fact]
    public async Task Attachment_IsStoredAndLinkedToTheTask()
    {
        await Enable();
        _mailbox.Add(Allowed, attachments: File("report.pdf", 120));

        await Poll();

        var attachment = Assert.Single(Assert.Single(await Tasks()).Attachments);
        Assert.Equal(("report.pdf", "application/pdf", 120L), (attachment.FileName, attachment.ContentType, attachment.SizeBytes));
        Assert.StartsWith("https://files.test/pjm/tasks/", attachment.Url);
        Assert.Equal(["report.pdf"], _storage.Uploaded);
    }

    [Fact]
    public async Task FilesNotDownloadedByTheClient_AreListedAsNotAttached()
    {
        await Enable();
        _mailbox.Add(Allowed, attachments:
        [
            NotDownloaded("huge.iso", "larger than 10 MB"),
            File("small.pdf"),
            NotDownloaded("eleventh.txt", "more than 10 files")
        ]);

        await Poll();

        var task = Assert.Single(await Tasks());
        Assert.Equal("small.pdf", Assert.Single(task.Attachments).FileName);
        Assert.Contains("Not attached:\n- huge.iso (larger than 10 MB)\n- eleventh.txt (more than 10 files)", task.Description.ReplaceLineEndings("\n"));
    }

    [Fact]
    public async Task StorageNotConfigured_ListsTheFiles_WithoutUploading()
    {
        await Enable();
        _storage.IsConfigured = false;
        _mailbox.Add(Allowed, attachments: File("report.pdf"));

        await Poll();

        var task = Assert.Single(await Tasks());
        Assert.Empty(task.Attachments);
        Assert.Contains("- report.pdf (file storage is not configured)", task.Description);
        Assert.Empty(_storage.Uploaded);
    }

    [Fact]
    public async Task UploadFailure_ListsTheFile_StillCreatesTheTask_AndIsNotRetried()
    {
        await Enable();
        _storage.FailFor.Add("broken.pdf");
        _mailbox.Add(Allowed, attachments: [File("broken.pdf"), File("ok.pdf")]);

        await Poll();
        await Poll();

        var task = Assert.Single(await Tasks());
        Assert.Equal("ok.pdf", Assert.Single(task.Attachments).FileName);
        Assert.Contains("- broken.pdf (upload failed: Upload rejected by storage.)", task.Description);
        Assert.Equal(["ok.pdf"], _storage.Uploaded);
    }
}
