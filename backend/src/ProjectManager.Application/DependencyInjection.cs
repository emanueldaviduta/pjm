using Microsoft.Extensions.DependencyInjection;
using ProjectManager.Application.Common;
using ProjectManager.Application.Interfaces;
using ProjectManager.Application.Services;

namespace ProjectManager.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IProjectMembershipService, ProjectMembershipService>();
        services.AddScoped<IMailSettingsService, MailSettingsService>();
        services.AddScoped<IEmailIngestionService, EmailIngestionService>();
        return services;
    }

    /// <summary>Gives the Admin role to the user named by <see cref="AdminOptions"/> (called at startup, after migrations).</summary>
    public static async Task EnsureConfiguredAdminAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IAppDbContext>()
            .EnsureConfiguredAdminAsync(scope.ServiceProvider.GetRequiredService<AdminOptions>());
    }
}
