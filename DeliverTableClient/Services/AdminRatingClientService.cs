using System.Net.Http.Json;
using DeliverTableClient.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableClient.Services;

public sealed class AdminRatingClientService(HttpClient httpClient) : IAdminRatingClientService
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<(List<AdminRestaurantRatingResponse>? Ratings, ErrorResponse? Error)> GetRestaurantRatingsAsync(
        CancellationToken ct = default)
    {
        using var response = await _httpClient.GetAsync($"{ApiRoutes.Admin.Ratings}/restaurants", ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        var items = await response.Content.ReadFromJsonAsync<List<AdminRestaurantRatingResponse>>(cancellationToken: ct);
        return items is not null
            ? (items, null)
            : (null, new ErrorResponse { Error = "Impossible de lire les avis restaurants", Status = (int)response.StatusCode });
    }

    public async Task<(List<AdminCustomerRatingResponse>? Ratings, ErrorResponse? Error)> GetCustomerRatingsAsync(
        CancellationToken ct = default)
    {
        using var response = await _httpClient.GetAsync($"{ApiRoutes.Admin.Ratings}/customers", ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        var items = await response.Content.ReadFromJsonAsync<List<AdminCustomerRatingResponse>>(cancellationToken: ct);
        return items is not null
            ? (items, null)
            : (null, new ErrorResponse { Error = "Impossible de lire les avis clients", Status = (int)response.StatusCode });
    }

    public async Task<(bool Success, ErrorResponse? Error)> DeleteAsync(int id, CancellationToken ct = default)
    {
        using var response = await _httpClient.DeleteAsync($"{ApiRoutes.Admin.Ratings}/{id}", ct);
        if (!response.IsSuccessStatusCode)
            return (false, await ReadError(response, ct));

        return (true, null);
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
