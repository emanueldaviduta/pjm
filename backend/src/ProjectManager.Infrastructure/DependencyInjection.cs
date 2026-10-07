using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProjectManager.Application.Interfaces;
using ProjectManager.Infrastructure.Persistence;
using ProjectManager.Infrastructure.Services;

namespace ProjectManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddDbContext<AppDb>(options =>
        {
            if (environment.IsDevelopment())
                options.UseSqlite(configuration["ConnectionLocalDb"]);
            else
                options.UseNpgsql(configuration["ConnectionString"]);
        });
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDb>());
        services.AddScoped<ITokenService, TokenService>();
        return services;
    }

    /// <summary>Applies pending EF Core migrations (called at startup, as before).</summary>
    public static void ApplyMigrations(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDb>().Database.Migrate();
    }
}
