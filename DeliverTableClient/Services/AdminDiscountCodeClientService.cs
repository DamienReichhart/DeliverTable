using System.Net.Http.Json;
using DeliverTableClient.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableClient.Services;

public sealed class AdminDiscountCodeClientService(HttpClient httpClient) : IAdminDiscountCodeClientService
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<(List<AdminDiscountCodeResponse>? DiscountCodes, ErrorResponse? Error)> GetAllAsync(
        CancellationToken ct = default)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync(ApiRoutes.Admin.DiscountCodes, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        List<AdminDiscountCodeResponse>? items = await response.Content.ReadFromJsonAsync<List<AdminDiscountCodeResponse>>(cancellationToken: ct);
        return items is not null
            ? (items, null)
            : (null, new ErrorResponse { Error = "Impossible de lire la liste des codes de réduction", Status = (int)response.StatusCode });
    }

    public async Task<(AdminDiscountCodeResponse? DiscountCode, ErrorResponse? Error)> GetByIdAsync(
        int id, CancellationToken ct = default)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync($"{ApiRoutes.Admin.DiscountCodes}/{id}", ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        AdminDiscountCodeResponse? item = await response.Content.ReadFromJsonAsync<AdminDiscountCodeResponse>(cancellationToken: ct);
        return (item, null);
    }

    public async Task<(AdminDiscountCodeResponse? DiscountCode, ErrorResponse? Error)> CreateAsync(
        AdminCreateDiscountCodeRequest request, CancellationToken ct = default)
    {
        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(ApiRoutes.Admin.DiscountCodes, request, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        AdminDiscountCodeResponse? item = await response.Content.ReadFromJsonAsync<AdminDiscountCodeResponse>(cancellationToken: ct);
        return (item, null);
    }

    public async Task<(AdminDiscountCodeResponse? DiscountCode, ErrorResponse? Error)> UpdateAsync(
        int id, AdminUpdateDiscountCodeRequest request, CancellationToken ct = default)
    {
        using HttpResponseMessage response = await _httpClient.PutAsJsonAsync($"{ApiRoutes.Admin.DiscountCodes}/{id}", request, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        AdminDiscountCodeResponse? item = await response.Content.ReadFromJsonAsync<AdminDiscountCodeResponse>(cancellationToken: ct);
        return (item, null);
    }

    public async Task<(bool Success, ErrorResponse? Error)> DeleteAsync(int id, CancellationToken ct = default)
    {
        using HttpResponseMessage response = await _httpClient.DeleteAsync($"{ApiRoutes.Admin.DiscountCodes}/{id}", ct);
        if (!response.IsSuccessStatusCode)
            return (false, await ReadError(response, ct));

        return (true, null);
    }

    public async Task<(List<AdminRedemptionResponse>? Redemptions, ErrorResponse? Error)> GetRedemptionsAsync(
        int id, CancellationToken ct = default)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync($"{ApiRoutes.Admin.DiscountCodes}/{id}/redemptions", ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        List<AdminRedemptionResponse>? items = await response.Content.ReadFromJsonAsync<List<AdminRedemptionResponse>>(cancellationToken: ct);
        return items is not null
            ? (items, null)
            : (null, new ErrorResponse { Error = "Impossible de lire les utilisations du code", Status = (int)response.StatusCode });
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
