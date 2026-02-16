using DeliverTableClient.Configuration.Interfaces;

namespace DeliverTableClient.Configuration;

/// <inheritdoc />
public sealed class ApiClientOptions : IApiClientOptions
{
    public string BaseUrl { get; init; } = "";
}