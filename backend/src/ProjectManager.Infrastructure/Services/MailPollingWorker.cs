using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectManager.Application.Interfaces;

namespace ProjectManager.Infrastructure.Services;

/// <summary>
/// Polls the inbound mailbox in the background: every configured interval while enabled, and every 30 seconds while
/// disabled so that enabling takes effect quickly. Each poll gets its own DI scope. The loop never throws.
/// </summary>
public class MailPollingWorker(IServiceScopeFactory scopes, ILogger<MailPollingWorker> logger) : BackgroundService
{
    private static readonly TimeSpan DisabledDelay = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = DisabledDelay;
            try
            {
                using var scope = scopes.CreateScope();
                var outcome = await scope.ServiceProvider.GetRequiredService<IEmailIngestionService>().PollOnceAsync(stoppingToken);
                if (outcome.Enabled)
                {
                    delay = TimeSpan.FromSeconds(outcome.PollIntervalSeconds);
                    if (outcome.Error != null)
                        logger.LogWarning("Mail poll failed: {Error}", outcome.Error);
                    else
                        logger.LogInformation("Mail poll created {Created} task(s)", outcome.Created);
                }
                else
                {
                    logger.LogDebug("Mail ingestion is disabled; checking again in {Delay}", DisabledDelay);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Mail poll crashed; retrying in {Delay}", delay);
            }

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
