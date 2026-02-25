using DeliverTableServer.Services;

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
        var result = await _sut.GetHealthAsync();

        Assert.That(result.Status, Is.EqualTo("Healthy"));
    }

    [Test]
    public async Task GetHealthAsync_ReturnsRecentUtcTimestamp()
    {
        var before = DateTime.UtcNow;

        var result = await _sut.GetHealthAsync();

        var after = DateTime.UtcNow;
        Assert.That(result.TimestampUtc, Is.InRange(before, after));
    }

    [Test]
    public async Task GetHealthAsync_CompletesWhenCancellationNotRequested()
    {
        using var cts = new CancellationTokenSource();

        var result = await _sut.GetHealthAsync(cts.Token);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Status, Is.EqualTo("Healthy"));
    }
}
