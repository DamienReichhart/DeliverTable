using System.Net.Http.Json;
using DeliverTableClient.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Dtos;

namespace DeliverTableClient.Services;

/// <inheritdoc />
public sealed class HealthApiClient : IHealthApiClient
{
    private readonly HttpClient _httpClient;

    public HealthApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public async Task<HealthResponse?> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(ApiRoutes.Health, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<HealthResponse>(cancellationToken);
        }
        catch
        {
            return null;
        }
    }
}
