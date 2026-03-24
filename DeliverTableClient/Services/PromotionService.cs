using System.Net.Http.Json;
using DeliverTableClient.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Promotion;

namespace DeliverTableClient.Services;

public sealed class PromotionService(HttpClient httpClient) : IPromotionService
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<(PaginatedResult<PromotionDto>?, ErrorResponse?)> GetByRestaurantAsync(int restaurantId, PromotionQuery query, CancellationToken ct = default)
    {
        try
        {
            var queryParams = new List<string>
            {
                $"PageNumber={query.PageNumber}",
                $"PageSize={query.PageSize}"
            };

            var url = $"api/v1/restaurant/{restaurantId}/promotions?{string.Join("&", queryParams)}";
            var result = await _httpClient.GetFromJsonAsync<PaginatedResult<PromotionDto>>(url, ct);
            return (result, null);
        }
        catch (Exception ex)
        {
            return (null, new ErrorResponse { Error = ex.Message });
        }
    }

    public async Task<(PromotionDto?, ErrorResponse?)> CreateAsync(int restaurantId, CreatePromotionRequest request, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/v1/restaurant/{restaurantId}/promotions", request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: ct);
            return (null, error);
        }

        var result = await response.Content.ReadFromJsonAsync<PromotionDto>(cancellationToken: ct);
        return (result, null);
    }

    public async Task<(PromotionDto?, ErrorResponse?)> UpdateAsync(int promotionId, UpdatePromotionRequest request, CancellationToken ct = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/v1/promotion/{promotionId}", request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: ct);
            return (null, error);
        }

        var result = await response.Content.ReadFromJsonAsync<PromotionDto>(cancellationToken: ct);
        return (result, null);
    }

    public async Task<(bool, ErrorResponse?)> DeleteAsync(int promotionId, CancellationToken ct = default)
    {
        var response = await _httpClient.DeleteAsync($"api/v1/promotion/{promotionId}", ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: ct);
            return (false, error);
        }

        return (true, null);
    }
}
