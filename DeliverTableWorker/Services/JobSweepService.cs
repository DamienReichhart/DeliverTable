using DeliverTableInfrastructure.Messaging;
using DeliverTableInfrastructure.Messaging.Messages;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableWorker.Services;

public class JobSweepService(
    IServiceScopeFactory scopeFactory,
    IMessagePublisher publisher,
    ILogger<JobSweepService> logger
) : BackgroundService
{
    private static readonly TimeSpan SweepInterval = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan PendingThreshold = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan ProcessingThreshold = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SweepAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during job sweep");
            }

            await Task.Delay(SweepInterval, stoppingToken);
        }
    }

    private async Task SweepAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IEmailJobRepository>();
        var now = DateTime.UtcNow;

        var stalePending = await repo.GetStaleJobsByStatusAsync(
            EmailJobStatus.Pending,
            now - PendingThreshold,
            ct
        );

        foreach (var job in stalePending)
        {
            try
            {
                await publisher.PublishAsync("email", new EmailJobMessage(job.Id));
                logger.LogWarning("Re-published stale Pending email job {JobId}", job.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to re-publish stale Pending job {JobId}",
                    job.Id
                );
            }
        }

        var staleProcessing = await repo.GetStaleJobsByStatusAsync(
            EmailJobStatus.Processing,
            now - ProcessingThreshold,
            ct
        );

        foreach (var job in staleProcessing)
        {
            job.Status = EmailJobStatus.Pending;
            job.RetryCount++;
            job.ProcessedAt = null;
            await repo.UpdateAsync(job, ct);
            logger.LogWarning(
                "Reset stale Processing email job {JobId} to Pending (retry {Retry})",
                job.Id,
                job.RetryCount
            );
        }
    }
}
