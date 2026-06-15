using DeliverTableServer.Common;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos;
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
        DateTime nowUtc = UtcNowOverride ?? DateTime.UtcNow;
        DateTime nowParis = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, ParisTz);
        DateTime prev = nowParis.AddMonths(-1);
        int year = prev.Year;
        int month = prev.Month;

        _logger.LogInformation("Running monthly commission statement job for {Year}-{Month:D2}", year, month);
        ServiceResult<CommissionStatementGenerationResultDto> result = await service.GenerateForPeriodAsync(year, month, context.CancellationToken);
        if (!result.IsSuccess)
        {
            _logger.LogError("Monthly commission job failed: {Reason}", result.Error?.Message);
            return;
        }
        CommissionStatementGenerationResultDto v = result.Value!;
        _logger.LogInformation(
            "Monthly commission job done: processed={Processed} created={Created} skipped={Skipped} failed={Failed}",
            v.RestaurantsProcessed, v.StatementsCreated, v.RestaurantsSkipped, v.Failures.Count);
    }
}
