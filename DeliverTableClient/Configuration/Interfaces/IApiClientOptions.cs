namespace DeliverTableClient.Configuration.Interfaces;

/// <summary>
/// Options for the API HTTP client (base URL, etc.). Bound from <see cref="IAppConfiguration"/> at startup.
/// </summary>
public interface IApiClientOptions
{
    /// <summary>Base URL of the API (e.g. https://localhost:7067). Empty means same origin as the client.</summary>
    string BaseUrl { get; }
}
