using DeliverTableServer.Common;
using DeliverTableServer.Controllers;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NUnit.Framework;

namespace DeliverTableTests.Server.Unit.Controllers;

[TestFixture]
public class AdminCommissionStatementControllerTests
{
    private ICommissionStatementService _service = null!;
    private AdminCommissionStatementController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _service = Substitute.For<ICommissionStatementService>();
        _sut = new AdminCommissionStatementController(_service);
    }

    [Test]
    public async Task Run_DefaultsToPreviousMonth_WhenBodyOmitted()
    {
        var nowParis = new DateTime(2026, 6, 5, 9, 0, 0, DateTimeKind.Utc); // mid-June UTC
        _sut.UtcNowOverride = nowParis;
        var expectedDto = new CommissionStatementGenerationResultDto { PeriodYear = 2026, PeriodMonth = 5 };
        _service.GenerateForPeriodAsync(2026, 5, default)
                .ReturnsForAnyArgs(ServiceResult<CommissionStatementGenerationResultDto>.Success(expectedDto));

        var result = await _sut.Run(body: null, default);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        await _service.Received(1).GenerateForPeriodAsync(2026, 5, default);
    }

    [Test]
    public async Task Run_UsesProvidedPeriod()
    {
        _service.GenerateForPeriodAsync(2026, 3, default)
                .Returns(ServiceResult<CommissionStatementGenerationResultDto>.Success(new()));

        var result = await _sut.Run(new CommissionStatementsRunRequest { Year = 2026, Month = 3 }, default);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        await _service.Received(1).GenerateForPeriodAsync(2026, 3, default);
    }
}
