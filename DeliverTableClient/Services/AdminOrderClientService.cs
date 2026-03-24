using System.Net.Http.Json;
using DeliverTableClient.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableClient.Services;

public sealed class AdminOrderClientService(HttpClient httpClient) : IAdminOrderClientService
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<(List<AdminOrderResponse>? Orders, ErrorResponse? Error)> GetAllAsync(
        CancellationToken ct = default)
    {
        using var response = await _httpClient.GetAsync(ApiRoutes.Admin.Orders, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        var items = await response.Content.ReadFromJsonAsync<List<AdminOrderResponse>>(cancellationToken: ct);
        return items is not null
            ? (items, null)
            : (null, new ErrorResponse { Error = "Impossible de lire la liste des commandes", Status = (int)response.StatusCode });
    }

    public async Task<(AdminOrderResponse? Order, ErrorResponse? Error)> GetByIdAsync(
        int id, CancellationToken ct = default)
    {
        using var response = await _httpClient.GetAsync($"{ApiRoutes.Admin.Orders}/{id}", ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        var item = await response.Content.ReadFromJsonAsync<AdminOrderResponse>(cancellationToken: ct);
        return (item, null);
    }

    public async Task<(AdminOrderResponse? Order, ErrorResponse? Error)> UpdateStatusAsync(
        int id, AdminUpdateOrderStatusRequest request, CancellationToken ct = default)
    {
        using var response = await _httpClient.PutAsJsonAsync($"{ApiRoutes.Admin.Orders}/{id}/status", request, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        var item = await response.Content.ReadFromJsonAsync<AdminOrderResponse>(cancellationToken: ct);
        return (item, null);
    }

    private static async Task<ErrorResponse> ReadError(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: ct);
            return error ?? new ErrorResponse { Error = "Une erreur est survenue", Status = (int)response.StatusCode };
        }
        catch
        {
            return new ErrorResponse { Error = "Une erreur est survenue", Status = (int)response.StatusCode };
        }
    }
}
