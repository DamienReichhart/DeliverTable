namespace DeliverTableSharedLibrary.Dtos;

/// <summary>
/// Response model for the health endpoint. Shared between server and client.
/// </summary>
public sealed class HealthResponse
{
    /// <summary>Current service health status (e.g. Healthy, Degraded, Unhealthy).</summary>
    public string Status { get; init; } = "Healthy";

    /// <summary>UTC timestamp when the health was checked.</summary>
    public DateTime TimestampUtc { get; init; }
}
