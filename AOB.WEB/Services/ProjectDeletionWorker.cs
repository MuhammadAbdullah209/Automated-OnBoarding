using AOB.BAL.Interfaces.Services;

namespace AOB.WEB.Services
{
    /// <summary>
    /// Background job: every few seconds, deletes projects whose deletion countdown has finished.
    /// ProjectDeletion:PollSeconds (default 30) sets how often it checks.
    /// </summary>
    public class ProjectDeletionWorker(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<ProjectDeletionWorker> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var seconds = Math.Max(5, configuration.GetValue("ProjectDeletion:PollSeconds", 30));
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(seconds));

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    try
                    {
                        using var scope = scopeFactory.CreateScope();
                        var purge = scope.ServiceProvider.GetRequiredService<IProjectPurgeService>();
                        var count = await purge.PurgeDueAsync(stoppingToken);
                        if (count > 0)
                        {
                            logger.LogInformation("Deleted {Count} project(s) whose deletion time had passed.", count);
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        // One failed run must not stop the job.
                        logger.LogError(ex, "Project deletion run failed.");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Shutting down.
            }
        }
    }
}
