using System.Net.Http.Json;
using DeliverTableClient.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableClient.Services;

public sealed class AdminPromotionClientService(HttpClient httpClient) : IAdminPromotionClientService
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<(List<AdminPromotionResponse>? Promotions, ErrorResponse? Error)> GetAllAsync(
        CancellationToken ct = default)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync(ApiRoutes.Admin.Promotions, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        List<AdminPromotionResponse>? items = await response.Content.ReadFromJsonAsync<List<AdminPromotionResponse>>(cancellationToken: ct);
        return items is not null
            ? (items, null)
            : (null, new ErrorResponse { Error = "Impossible de lire la liste des promotions", Status = (int)response.StatusCode });
    }

    public async Task<(AdminPromotionResponse? Promotion, ErrorResponse? Error)> GetByIdAsync(
        int id, CancellationToken ct = default)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync($"{ApiRoutes.Admin.Promotions}/{id}", ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        AdminPromotionResponse? item = await response.Content.ReadFromJsonAsync<AdminPromotionResponse>(cancellationToken: ct);
        return (item, null);
    }

    public async Task<(AdminPromotionResponse? Promotion, ErrorResponse? Error)> CreateAsync(
        AdminCreatePromotionRequest request, CancellationToken ct = default)
    {
        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(ApiRoutes.Admin.Promotions, request, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        AdminPromotionResponse? item = await response.Content.ReadFromJsonAsync<AdminPromotionResponse>(cancellationToken: ct);
        return (item, null);
    }

    public async Task<(AdminPromotionResponse? Promotion, ErrorResponse? Error)> UpdateAsync(
        int id, AdminUpdatePromotionRequest request, CancellationToken ct = default)
    {
        using HttpResponseMessage response = await _httpClient.PutAsJsonAsync($"{ApiRoutes.Admin.Promotions}/{id}", request, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        AdminPromotionResponse? item = await response.Content.ReadFromJsonAsync<AdminPromotionResponse>(cancellationToken: ct);
        return (item, null);
    }

    public async Task<(bool Success, ErrorResponse? Error)> DeleteAsync(int id, CancellationToken ct = default)
    {
        using HttpResponseMessage response = await _httpClient.DeleteAsync($"{ApiRoutes.Admin.Promotions}/{id}", ct);
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
