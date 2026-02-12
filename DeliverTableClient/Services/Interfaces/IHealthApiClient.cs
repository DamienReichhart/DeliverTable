using DeliverTableSharedLibrary.Dtos;

namespace DeliverTableClient.Services.Interfaces;

/// <summary>
/// Client for the health API. Extensible for additional health or monitoring endpoints.
/// </summary>
public interface IHealthApiClient
{
    /// <summary>
    /// Fetches the current health status from the server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The health response, or null if the request failed.</returns>
    Task<HealthResponse?> GetHealthAsync(CancellationToken cancellationToken = default);
}
