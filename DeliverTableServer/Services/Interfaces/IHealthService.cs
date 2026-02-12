using DeliverTableSharedLibrary.Dtos;

namespace DeliverTableServer.Services.Interfaces;

/// <summary>
/// Provides current health status for the application. Used by health endpoints and monitoring.
/// </summary>
public interface IHealthService
{
    /// <summary>
    /// Returns the current health status.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Current health response (status and timestamp).</returns>
    Task<HealthResponse> GetHealthAsync(CancellationToken cancellationToken = default);
}
