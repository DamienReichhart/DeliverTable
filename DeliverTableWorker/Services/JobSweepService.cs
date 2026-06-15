using DeliverTableInfrastructure.Messaging;
using DeliverTableInfrastructure.Messaging.Messages;
using DeliverTableInfrastructure.Models;
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
        using IServiceScope scope = scopeFactory.CreateScope();
        IEmailJobRepository repo = scope.ServiceProvider.GetRequiredService<IEmailJobRepository>();
        DateTime now = DateTime.UtcNow;

        List<EmailJob> stalePending = await repo.GetStaleJobsByStatusAsync(
            EmailJobStatus.Pending,
            now - PendingThreshold,
            ct
        );

        foreach (EmailJob job in stalePending)
        {
            try
            {
                await publisher.PublishAsync(MessagingExchanges.Email, new EmailJobMessage(job.Id));
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

        List<EmailJob> staleProcessing = await repo.GetStaleJobsByStatusAsync(
            EmailJobStatus.Processing,
            now - ProcessingThreshold,
            ct
        );

        foreach (EmailJob job in staleProcessing)
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
