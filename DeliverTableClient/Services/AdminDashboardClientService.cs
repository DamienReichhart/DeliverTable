using System.Net.Http.Json;
using DeliverTableClient.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableClient.Services;

public sealed class AdminDashboardClientService(HttpClient httpClient) : IAdminDashboardClientService
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<(AdminDashboardStatsResponse? Stats, ErrorResponse? Error)> GetStatsAsync(
        CancellationToken ct = default)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync(ApiRoutes.Admin.Dashboard, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        AdminDashboardStatsResponse? stats = await response.Content.ReadFromJsonAsync<AdminDashboardStatsResponse>(cancellationToken: ct);
        return stats is not null
            ? (stats, null)
            : (null, new ErrorResponse { Error = "Impossible de lire les statistiques du tableau de bord", Status = (int)response.StatusCode });
    }

    public async Task<(AdminDashboardAnalyticsResponse? Analytics, ErrorResponse? Error)> GetAnalyticsAsync(
        CancellationToken ct = default)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync(ApiRoutes.Admin.DashboardAnalytics, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        AdminDashboardAnalyticsResponse? analytics =
            await response.Content.ReadFromJsonAsync<AdminDashboardAnalyticsResponse>(cancellationToken: ct);
        return analytics is not null
            ? (analytics, null)
            : (null,
                new ErrorResponse
                {
                    Error = "Impossible de lire les données analytiques",
                    Status = (int)response.StatusCode
                });
    }

    private static async Task<ErrorResponse> ReadError(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: ct);
            return error ?? new ErrorResponse { Error = "Une erreur est survenue", Status = (int)response.StatusCode };
        }
        catch
        {
            return new ErrorResponse { Error = "Une erreur est survenue", Status = (int)response.StatusCode };
        }
    }
}
