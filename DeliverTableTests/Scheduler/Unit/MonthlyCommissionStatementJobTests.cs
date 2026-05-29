using DeliverTableScheduler.Jobs;
using DeliverTableServer.Common;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;
using Quartz;

namespace DeliverTableTests.Scheduler.Unit;

[TestFixture]
public class MonthlyCommissionStatementJobTests
{
    [Test]
    public async Task Execute_DelegatesToService_WithPreviousMonth()
    {
        var service = Substitute.For<ICommissionStatementService>();
        service.GenerateForPeriodAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
               .Returns(ServiceResult<CommissionStatementGenerationResultDto>.Success(new()));

        var sut = new MonthlyCommissionStatementJob(service, NullLoggerFactory.Instance)
        {
            UtcNowOverride = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
        };

        await sut.Execute(Substitute.For<IJobExecutionContext>());

        await service.Received(1).GenerateForPeriodAsync(2026, 5, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Execute_HandlesDstTransition_Correctly()
    {
        var service = Substitute.For<ICommissionStatementService>();
        service.GenerateForPeriodAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
               .Returns(ServiceResult<CommissionStatementGenerationResultDto>.Success(new()));

        var sut = new MonthlyCommissionStatementJob(service, NullLoggerFactory.Instance)
        {
            UtcNowOverride = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
        };

        await sut.Execute(Substitute.For<IJobExecutionContext>());

        await service.Received(1).GenerateForPeriodAsync(2026, 3, Arg.Any<CancellationToken>());
    }
}
