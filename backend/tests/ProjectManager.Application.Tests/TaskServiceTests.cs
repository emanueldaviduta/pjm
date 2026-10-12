using Microsoft.EntityFrameworkCore;
using ProjectManager.Application.DTOs;
using ProjectManager.Application.Services;
using ProjectManager.Domain.Entities;

namespace ProjectManager.Application.Tests;

public class TaskServiceTests : SqliteTestDb
{
    private readonly TaskService _service;

    public TaskServiceTests()
    {
        _service = new TaskService(Db);
    }

    private static TaskItemRequest NewTask(int projectId, string title = "T") => new()
    {
        ProjectId = projectId,
        Title = title,
        Description = "d",
        Status = Status.Created,
        Priority = Priority.Medium
    };

    [Fact]
    public async Task GetAll_ReturnsOnlyTasksOfTheUsersProjects()
    {
        var ana = await AddUser("Ana");
        var bob = await AddUser("Bob");
        var mine = await AddProject("P", (ana, MemberRoleId));
        var theirs = await AddProject("Q", (bob, OwnerRoleId));
        var myTask = await _service.CreateAsync(ana, NewTask(mine, "mine"));
        await _service.CreateAsync(bob, NewTask(theirs, "theirs"));

        var tasks = await _service.GetAllAsync(ana);

        Assert.Equal(myTask!.Id, Assert.Single(tasks).Id);
    }

    [Fact]
    public async Task GetById_TaskOfAnotherProject_IsNull()
    {
        var ana = await AddUser("Ana");
        var bob = await AddUser("Bob");
        var theirs = await AddProject("Q", (bob, OwnerRoleId));
        var task = await _service.CreateAsync(bob, NewTask(theirs));

        Assert.Null(await _service.GetByIdAsync(ana, task!.Id));
        Assert.NotNull(await _service.GetByIdAsync(bob, task.Id));
    }

    [Fact]
    public async Task Update_TaskOfAnotherProject_IsNull_AndUnchanged()
    {
        var ana = await AddUser("Ana");
        var bob = await AddUser("Bob");
        var theirs = await AddProject("Q", (bob, OwnerRoleId));
        var task = await _service.CreateAsync(bob, NewTask(theirs, "original"));

        var result = await _service.UpdateAsync(ana, task!.Id, NewTask(theirs, "hacked"));

        Assert.Null(result);
        Assert.Equal("original", (await Db.TaskItems.AsNoTracking().SingleAsync()).Title);
    }

    [Fact]
    public async Task Create_InAProjectTheUserDoesNotBelongTo_IsNull_AndStoresNothing()
    {
        var ana = await AddUser("Ana");
        var bob = await AddUser("Bob");
        var theirs = await AddProject("Q", (bob, OwnerRoleId));

        Assert.Null(await _service.CreateAsync(ana, NewTask(theirs)));
        Assert.Null(await _service.CreateAsync(ana, NewTask(999)));
        Assert.Empty(Db.TaskItems);
    }

    [Fact]
    public async Task TasksOfADeletedProject_AreHidden()
    {
        var ana = await AddUser("Ana");
        var project = await AddProject("P", (ana, OwnerRoleId));
        var task = await _service.CreateAsync(ana, NewTask(project));
        (await Db.Projects.FindAsync(project))!.IsDeleted = true;
        await Db.SaveChangesAsync();

        Assert.Empty(await _service.GetAllAsync(ana));
        Assert.Null(await _service.GetByIdAsync(ana, task!.Id));
    }

    [Fact]
    public async Task Member_CanCreateAndUpdate()
    {
        var ana = await AddUser("Ana");
        var bob = await AddUser("Bob");
        var project = await AddProject("P", (ana, OwnerRoleId), (bob, MemberRoleId));

        var created = await _service.CreateAsync(bob, NewTask(project, "by bob"));
        var updated = await _service.UpdateAsync(bob, created!.Id, NewTask(project, "edited"));

        Assert.Equal("edited", updated!.Title);
        Assert.NotNull(await _service.GetByIdAsync(ana, created.Id));
    }

    [Fact]
    public async Task Attachments_AreReturnedToMembers_AndEmptyWhenThereAreNone()
    {
        var ana = await AddUser("Ana");
        var project = await AddProject("P", (ana, MemberRoleId));
        var withFile = await _service.CreateAsync(ana, NewTask(project, "with file"));
        var plain = await _service.CreateAsync(ana, NewTask(project, "plain"));
        Db.TaskAttachments.Add(new TaskAttachment
        {
            TaskItemId = withFile!.Id, FileName = "report.pdf", ContentType = "application/pdf",
            SizeBytes = 120, Url = "https://files.test/report.pdf", PublicId = "pjm/tasks/report"
        });
        await Db.SaveChangesAsync();

        var read = await _service.GetByIdAsync(ana, withFile.Id);

        var attachment = Assert.Single(read!.Attachments);
        Assert.Equal(("report.pdf", "application/pdf", 120L, "https://files.test/report.pdf"),
            (attachment.FileName, attachment.ContentType, attachment.SizeBytes, attachment.Url));
        Assert.Empty((await _service.GetByIdAsync(ana, plain!.Id))!.Attachments);
        Assert.Single((await _service.GetAllAsync(ana)).Single(t => t.Id == withFile.Id).Attachments);
    }

    [Fact]
    public async Task Attachments_AreNotReturnedToNonMembers()
    {
        var ana = await AddUser("Ana");
        var bob = await AddUser("Bob");
        var project = await AddProject("P", (ana, OwnerRoleId));
        var task = await _service.CreateAsync(ana, NewTask(project));
        Db.TaskAttachments.Add(new TaskAttachment
        {
            TaskItemId = task!.Id, FileName = "secret.pdf", ContentType = "application/pdf",
            SizeBytes = 1, Url = "https://files.test/secret.pdf", PublicId = "pjm/tasks/secret"
        });
        await Db.SaveChangesAsync();

        Assert.Null(await _service.GetByIdAsync(bob, task.Id));
        Assert.Empty(await _service.GetAllAsync(bob));
    }
}
