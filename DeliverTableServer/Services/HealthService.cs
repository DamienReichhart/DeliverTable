using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos;

namespace DeliverTableServer.Services;

/// <inheritdoc />
public sealed class HealthService : IHealthService
{
    /// <inheritdoc />
    public Task<HealthResponse> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new HealthResponse
        {
            Status = "Healthy",
            TimestampUtc = DateTime.UtcNow
        });
    }
}