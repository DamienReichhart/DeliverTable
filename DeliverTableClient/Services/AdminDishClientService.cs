using System.Net.Http.Json;
using DeliverTableClient.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableClient.Services;

public sealed class AdminDishClientService(HttpClient httpClient) : IAdminDishClientService
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<(List<AdminDishResponse>? Dishes, ErrorResponse? Error)> GetAllAsync(
        CancellationToken ct = default)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync(ApiRoutes.Admin.Dishes, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        List<AdminDishResponse>? items = await response.Content.ReadFromJsonAsync<List<AdminDishResponse>>(cancellationToken: ct);
        return items is not null
            ? (items, null)
            : (null, new ErrorResponse { Error = "Impossible de lire la liste des plats", Status = (int)response.StatusCode });
    }

    public async Task<(AdminDishResponse? Dish, ErrorResponse? Error)> GetByIdAsync(
        int id, CancellationToken ct = default)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync($"{ApiRoutes.Admin.Dishes}/{id}", ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        AdminDishResponse? item = await response.Content.ReadFromJsonAsync<AdminDishResponse>(cancellationToken: ct);
        return (item, null);
    }

    public async Task<(AdminDishResponse? Dish, ErrorResponse? Error)> CreateAsync(
        AdminCreateDishRequest request, CancellationToken ct = default)
    {
        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(ApiRoutes.Admin.Dishes, request, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        AdminDishResponse? item = await response.Content.ReadFromJsonAsync<AdminDishResponse>(cancellationToken: ct);
        return (item, null);
    }

    public async Task<(AdminDishResponse? Dish, ErrorResponse? Error)> UpdateAsync(
        int id, AdminUpdateDishRequest request, CancellationToken ct = default)
    {
        using HttpResponseMessage response = await _httpClient.PutAsJsonAsync($"{ApiRoutes.Admin.Dishes}/{id}", request, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        AdminDishResponse? item = await response.Content.ReadFromJsonAsync<AdminDishResponse>(cancellationToken: ct);
        return (item, null);
    }

    public async Task<(bool Success, ErrorResponse? Error)> DeleteAsync(int id, CancellationToken ct = default)
    {
        using HttpResponseMessage response = await _httpClient.DeleteAsync($"{ApiRoutes.Admin.Dishes}/{id}", ct);
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
