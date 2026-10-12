using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectManager.Application.Interfaces;
using ProjectManager.Infrastructure.Persistence;
using ProjectManager.Infrastructure.Services;

namespace ProjectManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDb>(options => options.UseNpgsql(configuration["ConnectionString"]));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDb>());
        services.AddScoped<ITokenService, TokenService>();
        services.AddDataProtection().SetApplicationName("pjm").PersistKeysToDbContext<AppDb>();
        services.AddSingleton<ISecretProtector, DataProtectionSecretProtector>();
        services.AddSingleton<IMailboxClient, MailKitMailboxClient>();
        services.AddHostedService<MailPollingWorker>();
        var cloudinary = configuration.GetSection(CloudinaryOptions.Section);
        services.AddSingleton<IFileStorage>(new CloudinaryFileStorage(new CloudinaryOptions
        {
            CloudName = cloudinary["CloudName"],
            ApiKey = cloudinary["ApiKey"],
            ApiSecret = cloudinary["ApiSecret"]
        }));
        return services;
    }

    /// <summary>Applies pending EF Core migrations (called at startup, as before).</summary>
    public static void ApplyMigrations(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDb>().Database.Migrate();
    }
}
