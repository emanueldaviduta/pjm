using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using ProjectManager.Application.Common;
using ProjectManager.Application.DTOs;
using ProjectManager.Application.Interfaces;
using ProjectManager.Domain.Entities;

namespace ProjectManager.Application.Services;

public class AccountService(IAppDbContext context, ITokenService tokenService) : IAccountService
{
    public async Task<IReadOnlyList<AccountDto>> GetUsersAsync() =>
        await context.AppUsers.Select(u => ToAccountDto(u)).ToListAsync();

    public async Task<ServiceResult<UserDto>> LoginAsync(LoginDto loginDto)
    {
        var user = await context.AppUsers.FirstOrDefaultAsync(u => u.Email == loginDto.Email);
        if (user == null)
        {
            return ServiceResult<UserDto>.Invalid("Invalid username or password.");
        }

        if (!PasswordCheck(user.PasswordHash, user.PasswordSalt, loginDto.Password))
        {
            return ServiceResult<UserDto>.Invalid("Invalid username or password.");
        }

        return ServiceResult<UserDto>.Ok(ToUserDto(user));
    }

    public async Task<ServiceResult<UserDto>> RegisterAsync(RegisterDto registerDto)
    {
        var user = await context.AppUsers.FirstOrDefaultAsync(u => u.Email == registerDto.Email);
        if (user != null)
        {
            return ServiceResult<UserDto>.Invalid("User with this email already exists.");
        }

        using var hmac = new HMACSHA512();
        user = new AppUser
        {
            FirstName = registerDto.FirstName,
            LastName = registerDto.LastName,
            Email = registerDto.Email,
            PasswordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(registerDto.Password)),
            PasswordSalt = hmac.Key
        };

        context.AppUsers.Add(user);
        await context.SaveChangesAsync();

        return ServiceResult<UserDto>.Ok(ToUserDto(user));
    }

    public async Task<ServiceResult<AccountDto>> GetMeAsync(int userId)
    {
        var user = await context.AppUsers.FindAsync(userId);
        if (user == null)
        {
            return ServiceResult<AccountDto>.Unauthorized();
        }

        return ServiceResult<AccountDto>.Ok(ToAccountDto(user));
    }

    public async Task<ServiceResult<AccountDto>> UpdateMeAsync(int userId, UpdateAccountDto updateDto)
    {
        var user = await context.AppUsers.FindAsync(userId);
        if (user == null)
        {
            return ServiceResult<AccountDto>.Unauthorized();
        }

        user.FirstName = updateDto.FirstName.Trim();
        user.LastName = updateDto.LastName.Trim();
        await context.SaveChangesAsync();

        return ServiceResult<AccountDto>.Ok(ToAccountDto(user));
    }

    public async Task<ServiceResult> ChangePasswordAsync(int userId, ChangePasswordDto passwordDto)
    {
        var user = await context.AppUsers.FindAsync(userId);
        if (user == null)
        {
            return ServiceResult.Unauthorized();
        }

        if (!PasswordCheck(user.PasswordHash, user.PasswordSalt, passwordDto.CurrentPassword))
        {
            return ServiceResult.Invalid("Current password is incorrect.");
        }

        using var hmac = new HMACSHA512();
        user.PasswordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(passwordDto.NewPassword));
        user.PasswordSalt = hmac.Key;
        await context.SaveChangesAsync();

        return ServiceResult.Ok();
    }

    private static bool PasswordCheck(byte[] storedHash, byte[] storedSalt, string password)
    {
        using var hmac = new HMACSHA512(storedSalt);
        var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        for (int i = 0; i < computedHash.Length; i++)
        {
            if (computedHash[i] != storedHash[i])
            {
                return false;
            }
        }
        return true;
    }

    private UserDto ToUserDto(AppUser user) => new()
    {
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Token = tokenService.GenerateToken(user)
    };

    private static AccountDto ToAccountDto(AppUser user) => new()
    {
        Id = user.Id,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        CreatedAt = user.CreatedAt
    };
}
