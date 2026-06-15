using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos;

namespace DeliverTableTests.SharedLibrary.Unit.Dtos;

/// <summary>
///     Tests for <see cref="HealthResponse" />.
///     The default status value is a contract relied upon by health-check consumers.
/// </summary>
[TestFixture]
[Category("Contract")]
public class HealthResponseTests
{
    [Test]
    public void DefaultStatus_ShouldBeHealthy()
    {
        HealthResponse response = new HealthResponse();
        Assert.That(response.Status, Is.EqualTo(nameof(HealthStatus.Healthy)));
    }

    [Test]
    public void DefaultTimestampUtc_ShouldBeMinValue()
    {
        HealthResponse response = new HealthResponse();
        Assert.That(response.TimestampUtc, Is.EqualTo(default(DateTime)));
    }

    [TestCase(nameof(HealthStatus.Healthy))]
    [TestCase(nameof(HealthStatus.Degraded))]
    [TestCase(nameof(HealthStatus.Unhealthy))]
    public void Status_ShouldAcceptStandardHealthValues(string status)
    {
        HealthResponse response = new HealthResponse { Status = status };
        Assert.That(response.Status, Is.EqualTo(status));
    }

    [Test]
    public void TimestampUtc_ShouldPreserveAssignedValue()
    {
        DateTime expected = new DateTime(2026, 2, 25, 12, 0, 0, DateTimeKind.Utc);
        HealthResponse response = new HealthResponse { TimestampUtc = expected };
        Assert.That(response.TimestampUtc, Is.EqualTo(expected));
    }
}
