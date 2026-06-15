using DeliverTableServer.Services;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos;

namespace DeliverTableTests.Server.Unit.Services;

[TestFixture]
public class HealthServiceTests
{
    private HealthService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new HealthService();
    }

    [Test]
    public async Task GetHealthAsync_ReturnsHealthyStatus()
    {
        HealthResponse result = await _sut.GetHealthAsync();

        Assert.That(result.Status, Is.EqualTo(nameof(HealthStatus.Healthy)));
    }

    [Test]
    public async Task GetHealthAsync_ReturnsRecentUtcTimestamp()
    {
        DateTime before = DateTime.UtcNow;

        HealthResponse result = await _sut.GetHealthAsync();

        DateTime after = DateTime.UtcNow;
        Assert.That(result.TimestampUtc, Is.InRange(before, after));
    }

    [Test]
    public async Task GetHealthAsync_CompletesWhenCancellationNotRequested()
    {
        using CancellationTokenSource cts = new CancellationTokenSource();

        HealthResponse result = await _sut.GetHealthAsync(cts.Token);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Status, Is.EqualTo(nameof(HealthStatus.Healthy)));
    }
}
