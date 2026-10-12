using Microsoft.EntityFrameworkCore;
using ProjectManager.Application.Interfaces;
using ProjectManager.Domain.Entities;

namespace ProjectManager.Application.Common;

/// <summary>Configuration for the first admin: the user with this email is given the Admin role.</summary>
public class AdminOptions
{
    public string? Email { get; set; }

    /// <returns>True when an admin email is configured and equals <paramref name="email"/>, ignoring case.</returns>
    public bool Matches(string email) =>
        !string.IsNullOrWhiteSpace(Email) && string.Equals(Email.Trim(), email.Trim(), StringComparison.OrdinalIgnoreCase);
}

/// <summary>The global Admin role, checked from the database on every request so revocation takes effect at once.</summary>
public static class AdminAccess
{
    public static Task<bool> IsAdminAsync(this IAppDbContext context, int userId) =>
        context.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.Role!.Name == RoleNames.Admin);

    public static Task<int> AdminRoleIdAsync(this IAppDbContext context) =>
        context.Roles.Where(r => r.Name == RoleNames.Admin).Select(r => r.Id).SingleAsync();

    /// <summary>Gives Admin to the configured user if they exist and do not hold it yet; does nothing without a configured email.</summary>
    public static async Task EnsureConfiguredAdminAsync(this IAppDbContext context, AdminOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Email))
        {
            return;
        }

        var email = options.Email.Trim().ToLower();
        var userId = await context.AppUsers.Where(u => u.Email.ToLower() == email).Select(u => (int?)u.Id).FirstOrDefaultAsync();
        if (userId == null || await context.IsAdminAsync(userId.Value))
        {
            return;
        }

        context.UserRoles.Add(new UserRole { UserId = userId.Value, RoleId = await context.AdminRoleIdAsync() });
        await context.SaveChangesAsync();
    }
}
