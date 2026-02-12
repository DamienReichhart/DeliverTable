namespace DeliverTableClient.Configuration.Interfaces;

/// <summary>
/// Centralized application configuration for the Blazor client.
/// Loaded once at startup from wwwroot/appconfig.json.
/// Do not store secrets here; the file is served to the browser.
/// </summary>
public interface IAppConfiguration
{
    /// <summary>
    /// Whether configuration has been loaded from appconfig.json.
    /// </summary>
    bool IsLoaded { get; }

    /// <summary>
    /// Base URL of the API (e.g. https://localhost:7067 or empty for same origin).
    /// </summary>
    string ApiBaseUrl { get; }

    /// <summary>
    /// Environment name (e.g. Development, Production) for display or feature flags.
    /// </summary>
    string Environment { get; }

    /// <summary>
    /// Loads configuration from appconfig.json (same origin). Idempotent after first successful load.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LoadAsync(CancellationToken cancellationToken = default);
}
