using System.Net;
using System.Net.Http.Json;
using DeliverTableClient.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Rating;

namespace DeliverTableClient.Services;

public sealed class RatingClientService(HttpClient httpClient) : IRatingClientService
{
    private readonly HttpClient _httpClient = httpClient;

    private static string Url(int orderId) => $"{ApiRoutes.Order.Base}/{orderId}/rating";

    public async Task<(RatingDto?, ErrorResponse?)> GetByOrderAsync(
        int orderId,
        CancellationToken ct = default
    )
    {
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(Url(orderId), ct);
            if (response.StatusCode == HttpStatusCode.NotFound)
                return (null, null);

            if (!response.IsSuccessStatusCode)
            {
                ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(
                    cancellationToken: ct
                );
                return (null, error);
            }

            RatingDto? result = await response.Content.ReadFromJsonAsync<RatingDto>(
                cancellationToken: ct
            );
            return (result, null);
        }
        catch (Exception ex)
        {
            return (null, new ErrorResponse { Error = ex.Message });
        }
    }

    public async Task<(RatingDto?, ErrorResponse?)> CreateAsync(
        int orderId,
        CreateRatingRequest request,
        CancellationToken ct = default
    )
    {
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync(Url(orderId), request, ct);
        if (!response.IsSuccessStatusCode)
        {
            ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(
                cancellationToken: ct
            );
            return (null, error);
        }

        RatingDto? result = await response.Content.ReadFromJsonAsync<RatingDto>(cancellationToken: ct);
        return (result, null);
    }

    public async Task<(RatingDto?, ErrorResponse?)> UpdateAsync(
        int orderId,
        UpdateRatingRequest request,
        CancellationToken ct = default
    )
    {
        HttpResponseMessage response = await _httpClient.PutAsJsonAsync(Url(orderId), request, ct);
        if (!response.IsSuccessStatusCode)
        {
            ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(
                cancellationToken: ct
            );
            return (null, error);
        }

        RatingDto? result = await response.Content.ReadFromJsonAsync<RatingDto>(cancellationToken: ct);
        return (result, null);
    }

    public async Task<(bool, ErrorResponse?)> DeleteAsync(
        int orderId,
        CancellationToken ct = default
    )
    {
        HttpResponseMessage response = await _httpClient.DeleteAsync(Url(orderId), ct);
        if (!response.IsSuccessStatusCode)
        {
            ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(
                cancellationToken: ct
            );
            return (false, error);
        }

        return (true, null);
    }
}
