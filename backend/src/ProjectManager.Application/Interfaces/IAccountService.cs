using ProjectManager.Application.Common;
using ProjectManager.Application.DTOs;

namespace ProjectManager.Application.Interfaces;

public interface IAccountService
{
    Task<IReadOnlyList<AccountDto>> GetUsersAsync();

    /// <returns>Invalid with "Invalid username or password." for an unknown email or a wrong password.</returns>
    Task<ServiceResult<UserDto>> LoginAsync(LoginDto loginDto);

    /// <returns>Invalid when the email is already registered.</returns>
    Task<ServiceResult<UserDto>> RegisterAsync(RegisterDto registerDto);

    /// <returns>Unauthorized when no user has that id.</returns>
    Task<ServiceResult<AccountDto>> GetMeAsync(int userId);

    /// <returns>Unauthorized when no user has that id.</returns>
    Task<ServiceResult<AccountDto>> UpdateMeAsync(int userId, UpdateAccountDto updateDto);

    /// <returns>Unauthorized when no user has that id; Invalid when the current password is wrong.</returns>
    Task<ServiceResult> ChangePasswordAsync(int userId, ChangePasswordDto passwordDto);
}
