using DeliverTableServer.Controllers;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace DeliverTableTests.Server.Unit.Controllers;

[TestFixture]
public class HealthControllerTests
{
    private IHealthService _healthService = null!;
    private HealthController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _healthService = Substitute.For<IHealthService>();
        _sut = new HealthController(_healthService);
    }

    [Test]
    public async Task Get_ReturnsOkWithHealthResponse()
    {
        var expected = new HealthResponse
        {
            Status = nameof(HealthStatus.Healthy),
            TimestampUtc = DateTime.UtcNow
        };
        _healthService.GetHealthAsync(Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Get(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var ok = (OkObjectResult)result;
        Assert.That(ok.Value, Is.SameAs(expected));
    }

    [Test]
    public async Task Get_ForwardsCancellationTokenToService()
    {
        using var cts = new CancellationTokenSource();
        _healthService.GetHealthAsync(Arg.Any<CancellationToken>())
            .Returns(new HealthResponse { Status = nameof(HealthStatus.Healthy), TimestampUtc = DateTime.UtcNow });

        await _sut.Get(cts.Token);

        await _healthService.Received(1).GetHealthAsync(cts.Token);
    }

}
