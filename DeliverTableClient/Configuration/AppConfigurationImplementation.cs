using System.Text.Json;
using DeliverTableClient.Configuration.Interfaces;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace DeliverTableClient.Configuration;

/// <summary>
///     Loads and holds client configuration from wwwroot/appconfig.json (or appconfig.Development.json in Development).
///     Uses a same-origin HttpClient to fetch the file at startup.
/// </summary>
public sealed class AppConfigurationImplementation : IAppConfiguration
{
    private readonly HttpClient _configHttpClient;
    private readonly string _fallbackBaseAddress;
    private readonly IWebAssemblyHostEnvironment _hostEnvironment;
    private readonly object _lock = new();
    private bool _loaded;

    public AppConfigurationImplementation(
        HttpClient configHttpClient,
        IWebAssemblyHostEnvironment hostEnvironment,
        string fallbackBaseAddress)
    {
        _configHttpClient = configHttpClient ?? throw new ArgumentNullException(nameof(configHttpClient));
        _hostEnvironment = hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));
        _fallbackBaseAddress = fallbackBaseAddress ?? "";
    }

    public bool IsLoaded
    {
        get
        {
            lock (_lock)
            {
                return _loaded;
            }
        }
    }

    public string ApiBaseUrl { get; private set; } = "";

    public string Environment { get; private set; } = "";

    /// <inheritdoc />
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (IsLoaded)
            return;

        var fileName = _hostEnvironment.IsDevelopment()
            ? AppConfigurationOptions.DevelopmentConfigFileName
            : AppConfigurationOptions.ConfigFileName;

        try
        {
            var response = await _configHttpClient.GetAsync(fileName, cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                var options = JsonSerializer.Deserialize<AppConfigurationOptions>(json);

                lock (_lock)
                {
                    ApiBaseUrl = options?.Api?.BaseUrl?.TrimEnd('/') ?? "";
                    Environment = options?.Environment?.Trim() ?? "";
                    _loaded = true;
                }
            }
            else if (_hostEnvironment.IsDevelopment() && fileName == AppConfigurationOptions.DevelopmentConfigFileName)
            {
                // Fallback: try base appconfig.json when appconfig.Development.json is missing (e.g. first run).
                var fallback = await _configHttpClient.GetAsync(
                    AppConfigurationOptions.ConfigFileName,
                    cancellationToken).ConfigureAwait(false);
                if (fallback.IsSuccessStatusCode)
                {
                    var json = await fallback.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    var options = JsonSerializer.Deserialize<AppConfigurationOptions>(json);
                    lock (_lock)
                    {
                        ApiBaseUrl = options?.Api?.BaseUrl?.TrimEnd('/') ?? "";
                        Environment = options?.Environment?.Trim() ?? "";
                        _loaded = true;
                    }
                }
                else
                {
                    lock (_lock)
                    {
                        _loaded = true;
                    }
                }
            }
            else
            {
                lock (_lock)
                {
                    _loaded = true;
                }
            }
        }
        catch
        {
            // Keep defaults: empty ApiBaseUrl (same origin), empty Environment
            lock (_lock)
            {
                _loaded = true;
            }
        }

        // If no base URL was configured, use same origin (fallback).
        if (string.IsNullOrEmpty(ApiBaseUrl))
            ApiBaseUrl = _fallbackBaseAddress;
    }
}