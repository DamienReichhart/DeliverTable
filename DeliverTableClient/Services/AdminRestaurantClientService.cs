using System.Net.Http.Json;
using DeliverTableClient.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableClient.Services;

public sealed class AdminRestaurantClientService(HttpClient httpClient) : IAdminRestaurantClientService
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<(List<AdminRestaurantResponse>? Restaurants, ErrorResponse? Error)> GetAllAsync(
        CancellationToken ct = default)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync(ApiRoutes.Admin.Restaurants, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        List<AdminRestaurantResponse>? items = await response.Content.ReadFromJsonAsync<List<AdminRestaurantResponse>>(cancellationToken: ct);
        return items is not null
            ? (items, null)
            : (null, new ErrorResponse { Error = "Impossible de lire la liste des restaurants", Status = (int)response.StatusCode });
    }

    public async Task<(AdminRestaurantResponse? Restaurant, ErrorResponse? Error)> GetByIdAsync(
        int id, CancellationToken ct = default)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync($"{ApiRoutes.Admin.Restaurants}/{id}", ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        AdminRestaurantResponse? item = await response.Content.ReadFromJsonAsync<AdminRestaurantResponse>(cancellationToken: ct);
        return (item, null);
    }

    public async Task<(AdminRestaurantResponse? Restaurant, ErrorResponse? Error)> UpdateAsync(
        int id, AdminUpdateRestaurantRequest request, CancellationToken ct = default)
    {
        using HttpResponseMessage response = await _httpClient.PutAsJsonAsync($"{ApiRoutes.Admin.Restaurants}/{id}", request, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        AdminRestaurantResponse? item = await response.Content.ReadFromJsonAsync<AdminRestaurantResponse>(cancellationToken: ct);
        return (item, null);
    }

    public async Task<(bool Success, ErrorResponse? Error)> DeleteAsync(int id, CancellationToken ct = default)
    {
        using HttpResponseMessage response = await _httpClient.DeleteAsync($"{ApiRoutes.Admin.Restaurants}/{id}", ct);
        if (!response.IsSuccessStatusCode)
            return (false, await ReadError(response, ct));

        return (true, null);
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
