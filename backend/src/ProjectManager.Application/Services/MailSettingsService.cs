using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using ProjectManager.Application.Common;
using ProjectManager.Application.DTOs;
using ProjectManager.Application.Interfaces;
using ProjectManager.Domain.Entities;

namespace ProjectManager.Application.Services;

public class MailSettingsService(
    IAppDbContext context,
    ISecretProtector protector,
    IMailboxClient mailbox,
    IFileStorage fileStorage) : IMailSettingsService
{
    public async Task<ServiceResult<MailSettingsDto>> GetAsync(int callerId)
    {
        if (!await context.IsAdminAsync(callerId))
        {
            return ServiceResult<MailSettingsDto>.Forbidden();
        }

        var settings = await context.MailSettings.AsNoTracking().FirstOrDefaultAsync() ?? new MailSettings();
        return ServiceResult<MailSettingsDto>.Ok(ToDto(settings));
    }

    public async Task<ServiceResult<MailSettingsDto>> SaveAsync(int callerId, SaveMailSettingsRequest request)
    {
        if (!await context.IsAdminAsync(callerId))
        {
            return ServiceResult<MailSettingsDto>.Forbidden();
        }

        var settings = await context.MailSettings.FirstOrDefaultAsync();
        var isNew = settings == null;
        settings ??= new MailSettings();

        var senders = NormalizeSenders(request.AllowedSenders);
        var folder = string.IsNullOrWhiteSpace(request.Folder) ? "INBOX" : request.Folder.Trim();
        var hasPassword = !string.IsNullOrEmpty(request.Password) || settings.ProtectedPassword != null;
        if (request.Enabled && await Validate(request, senders, hasPassword) is { } problem)
        {
            return ServiceResult<MailSettingsDto>.Invalid(problem);
        }

        // Only mail that arrives from now on is imported: when (re-)enabled or pointed at another mailbox,
        // drop the cursor so the next poll takes a fresh baseline.
        var host = request.Host.Trim();
        var username = request.Username.Trim();
        if ((request.Enabled && !settings.Enabled) || host != settings.Host || username != settings.Username || folder != settings.Folder)
        {
            settings.MailboxKey = null;
            settings.UidValidity = null;
            settings.LastUid = null;
        }

        settings.Enabled = request.Enabled;
        settings.Host = host;
        settings.Port = request.Port;
        settings.Security = request.Security;
        settings.Username = username;
        settings.Folder = folder;
        settings.AllowedSenders = senders;
        settings.ProjectId = request.ProjectId;
        settings.PollIntervalSeconds = request.PollIntervalSeconds;
        if (!string.IsNullOrEmpty(request.Password))
        {
            settings.ProtectedPassword = protector.Protect(request.Password);
        }

        if (isNew)
        {
            context.MailSettings.Add(settings);
        }
        await context.SaveChangesAsync();

        return ServiceResult<MailSettingsDto>.Ok(ToDto(settings));
    }

    public async Task<ServiceResult<MailTestResultDto>> TestAsync(int callerId, SaveMailSettingsRequest request)
    {
        if (!await context.IsAdminAsync(callerId))
        {
            return ServiceResult<MailTestResultDto>.Forbidden();
        }

        var password = request.Password;
        if (string.IsNullOrEmpty(password))
        {
            var stored = await context.MailSettings.AsNoTracking().Select(m => m.ProtectedPassword).FirstOrDefaultAsync();
            password = stored == null ? null : protector.Unprotect(stored);
        }

        string? problem =
            string.IsNullOrWhiteSpace(request.Host) ? "A host is required." :
            request.Port is < 1 or > 65535 ? "The port must be between 1 and 65535." :
            string.IsNullOrWhiteSpace(request.Username) ? "A username is required." :
            string.IsNullOrEmpty(password) ? "A password is required." :
            null;
        if (problem != null)
        {
            return ServiceResult<MailTestResultDto>.Ok(new MailTestResultDto { Success = false, Message = problem });
        }

        var folder = string.IsNullOrWhiteSpace(request.Folder) ? "INBOX" : request.Folder.Trim();
        var result = await mailbox.TestAsync(new MailboxConnection(
            request.Host.Trim(), request.Port, request.Security, request.Username.Trim(), password!, folder));
        return ServiceResult<MailTestResultDto>.Ok(new MailTestResultDto { Success = result.Success, Message = result.Message });
    }

    public async Task<ServiceResult<IReadOnlyList<SettingsUserDto>>> GetUsersAsync(int callerId)
    {
        if (!await context.IsAdminAsync(callerId))
        {
            return ServiceResult<IReadOnlyList<SettingsUserDto>>.Forbidden();
        }

        var users = await context.AppUsers
            .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
            .Select(u => new SettingsUserDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                IsAdmin = context.UserRoles.Any(ur => ur.UserId == u.Id && ur.Role!.Name == RoleNames.Admin)
            })
            .ToListAsync();
        return ServiceResult<IReadOnlyList<SettingsUserDto>>.Ok(users);
    }

    public async Task<ServiceResult<IReadOnlyList<ProjectOptionDto>>> GetProjectsAsync(int callerId)
    {
        if (!await context.IsAdminAsync(callerId))
        {
            return ServiceResult<IReadOnlyList<ProjectOptionDto>>.Forbidden();
        }

        var projects = await context.Projects
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.Name)
            .Select(p => new ProjectOptionDto { Id = p.Id, Name = p.Name })
            .ToListAsync();
        return ServiceResult<IReadOnlyList<ProjectOptionDto>>.Ok(projects);
    }

    /// <returns>A message naming the first problem, or null when the enabled settings are complete.</returns>
    private async Task<string?> Validate(SaveMailSettingsRequest request, List<string> senders, bool hasPassword)
    {
        if (string.IsNullOrWhiteSpace(request.Host)) return "A host is required.";
        if (request.Port is < 1 or > 65535) return "The port must be between 1 and 65535.";
        if (string.IsNullOrWhiteSpace(request.Username)) return "A username is required.";
        if (!hasPassword) return "A password is required.";
        if (senders.Count == 0) return "At least one allowed sender is required.";
        if (senders.FirstOrDefault(s => !IsPlainAddress(s)) is { } invalid) return $"Invalid sender address: {invalid}";
        if (request.ProjectId == null) return "A target project is required.";
        if (!await context.Projects.AnyAsync(p => p.Id == request.ProjectId && !p.IsDeleted)) return "The target project does not exist.";
        if (request.PollIntervalSeconds is < MailLimits.MinPollIntervalSeconds or > MailLimits.MaxPollIntervalSeconds)
        {
            return $"The poll interval must be between {MailLimits.MinPollIntervalSeconds} and {MailLimits.MaxPollIntervalSeconds} seconds.";
        }
        return null;
    }

    private static List<string> NormalizeSenders(IEnumerable<string>? senders) =>
        (senders ?? [])
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    // Only a bare address ("a@b.c"), not "Name <a@b.c>".
    private static bool IsPlainAddress(string value) =>
        MailAddress.TryCreate(value, out var address) && address.Address == value && address.Host.Contains('.');

    private MailSettingsDto ToDto(MailSettings settings) => new()
    {
        Enabled = settings.Enabled,
        Host = settings.Host,
        Port = settings.Port,
        Security = settings.Security,
        Username = settings.Username,
        HasPassword = settings.ProtectedPassword != null,
        Folder = settings.Folder,
        AllowedSenders = [.. settings.AllowedSenders],
        ProjectId = settings.ProjectId,
        PollIntervalSeconds = settings.PollIntervalSeconds,
        LastPollAt = settings.LastPollAt,
        LastPollError = settings.LastPollError,
        LastPollCreated = settings.LastPollCreated,
        AttachmentStorageConfigured = fileStorage.IsConfigured
    };
}
