using ProjectManager.Application.Common;
using ProjectManager.Application.DTOs;

namespace ProjectManager.Application.Interfaces;

/// <summary>Admin-only settings: the inbound mailbox, plus the user and project lists the settings pages need. Non-admins get Forbidden.</summary>
public interface IMailSettingsService
{
    Task<ServiceResult<MailSettingsDto>> GetAsync(int callerId);

    /// <returns>The saved settings; Invalid with a message naming the problem when enabled settings are incomplete.</returns>
    Task<ServiceResult<MailSettingsDto>> SaveAsync(int callerId, SaveMailSettingsRequest request);

    /// <summary>Tries the submitted connection (stored password when none is sent). Changes nothing.</summary>
    Task<ServiceResult<MailTestResultDto>> TestAsync(int callerId, SaveMailSettingsRequest request);

    Task<ServiceResult<IReadOnlyList<SettingsUserDto>>> GetUsersAsync(int callerId);

    Task<ServiceResult<IReadOnlyList<ProjectOptionDto>>> GetProjectsAsync(int callerId);
}
