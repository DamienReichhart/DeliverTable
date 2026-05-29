using DeliverTableServer.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Quartz;

namespace DeliverTableScheduler.Jobs;

[DisallowConcurrentExecution]
public sealed class MonthlyCommissionStatementJob(
    ICommissionStatementService service,
    ILoggerFactory loggerFactory) : IJob
{
    private static readonly TimeZoneInfo ParisTz =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris");
    private readonly ILogger _logger = loggerFactory.CreateLogger<MonthlyCommissionStatementJob>();

    public DateTime? UtcNowOverride { get; set; }

    public async Task Execute(IJobExecutionContext context)
    {
        var nowUtc = UtcNowOverride ?? DateTime.UtcNow;
        var nowParis = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, ParisTz);
        var prev = nowParis.AddMonths(-1);
        var year = prev.Year;
        var month = prev.Month;

        _logger.LogInformation("Running monthly commission statement job for {Year}-{Month:D2}", year, month);
        var result = await service.GenerateForPeriodAsync(year, month, context.CancellationToken);
        if (!result.IsSuccess)
        {
            _logger.LogError("Monthly commission job failed: {Reason}", result.Error?.Message);
            return;
        }
        var v = result.Value!;
        _logger.LogInformation(
            "Monthly commission job done: processed={Processed} created={Created} skipped={Skipped} failed={Failed}",
            v.RestaurantsProcessed, v.StatementsCreated, v.RestaurantsSkipped, v.Failures.Count);
    }
}
