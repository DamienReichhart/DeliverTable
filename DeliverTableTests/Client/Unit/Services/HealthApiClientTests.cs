using System.Net;
using DeliverTableClient.Services;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableTests.Client.Helpers;

namespace DeliverTableTests.Client.Unit.Services;

/// <summary>
///     Tests for <see cref="HealthApiClient" />.
///     Verifies correct endpoint usage, response deserialization,
///     and graceful degradation on failure.
/// </summary>
[TestFixture]
public class HealthApiClientTests
{
    private MockHttpMessageHandler _httpHandler = null!;
    private HttpClient _httpClient = null!;
    private HealthApiClient _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _httpHandler = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_httpHandler) { BaseAddress = new Uri("http://localhost/") };
        _sut = new HealthApiClient(_httpClient);
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient.Dispose();
        _httpHandler.Dispose();
    }

    #region GetHealthAsync

    [Test]
    public async Task GetHealthAsync_WithSuccessfulResponse_ReturnsHealthResponse()
    {
        var expected = new HealthResponse
        {
            Status = nameof(HealthStatus.Healthy),
            TimestampUtc = new DateTime(2026, 2, 25, 12, 0, 0, DateTimeKind.Utc)
        };
        _httpHandler.QueueJsonResponse(expected);

        var result = await _sut.GetHealthAsync();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Status, Is.EqualTo(nameof(HealthStatus.Healthy)));
            Assert.That(result.TimestampUtc, Is.EqualTo(expected.TimestampUtc));
        });
    }

    [Test]
    public async Task GetHealthAsync_CallsCorrectEndpoint()
    {
        _httpHandler.QueueJsonResponse(new HealthResponse());

        await _sut.GetHealthAsync();

        Assert.Multiple(() =>
        {
            Assert.That(_httpHandler.SentRequests, Has.Count.EqualTo(1));
            Assert.That(_httpHandler.SentRequests[0].Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(_httpHandler.SentRequests[0].RequestUri!.PathAndQuery,
                Does.EndWith(ApiRoutes.Health));
        });
    }

    [Test]
    public async Task GetHealthAsync_WithServerError_ReturnsNull()
    {
        _httpHandler.QueueErrorResponse(HttpStatusCode.InternalServerError);

        var result = await _sut.GetHealthAsync();

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetHealthAsync_WithNotFound_ReturnsNull()
    {
        _httpHandler.QueueErrorResponse(HttpStatusCode.NotFound);

        var result = await _sut.GetHealthAsync();

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetHealthAsync_SupportsCancellation()
    {
        _httpHandler.QueueJsonResponse(new HealthResponse());
        using var cts = new CancellationTokenSource();

        var result = await _sut.GetHealthAsync(cts.Token);

        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public async Task GetHealthAsync_WithDegradedStatus_ReturnsDegradedResponse()
    {
        var degraded = new HealthResponse
        {
            Status = nameof(HealthStatus.Degraded),
            TimestampUtc = DateTime.UtcNow
        };
        _httpHandler.QueueJsonResponse(degraded);

        var result = await _sut.GetHealthAsync();

        Assert.That(result!.Status, Is.EqualTo(nameof(HealthStatus.Degraded)));
    }

    #endregion
}
