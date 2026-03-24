using System.Net.Http.Json;
using DeliverTableClient.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.DiscountCode;

namespace DeliverTableClient.Services;

public sealed class DiscountCodeClientService(HttpClient httpClient) : IDiscountCodeClientService
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<(PaginatedResult<DiscountCodeDto>?, ErrorResponse?)> GetByRestaurantAsync(int restaurantId, DiscountCodeQuery query, CancellationToken ct = default)
    {
        try
        {
            var queryParams = new List<string>
            {
                $"PageNumber={query.PageNumber}",
                $"PageSize={query.PageSize}"
            };

            var url = $"api/v1/restaurant/{restaurantId}/discount-codes?{string.Join("&", queryParams)}";
            var result = await _httpClient.GetFromJsonAsync<PaginatedResult<DiscountCodeDto>>(url, ct);
            return (result, null);
        }
        catch (Exception ex)
        {
            return (null, new ErrorResponse { Error = ex.Message });
        }
    }

    public async Task<(DiscountCodeDto?, ErrorResponse?)> CreateAsync(int restaurantId, CreateDiscountCodeRequest request, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/v1/restaurant/{restaurantId}/discount-codes", request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: ct);
            return (null, error);
        }

        var result = await response.Content.ReadFromJsonAsync<DiscountCodeDto>(cancellationToken: ct);
        return (result, null);
    }

    public async Task<(DiscountCodeDto?, ErrorResponse?)> UpdateAsync(int discountCodeId, UpdateDiscountCodeRequest request, CancellationToken ct = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/v1/discount-code/{discountCodeId}", request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: ct);
            return (null, error);
        }

        var result = await response.Content.ReadFromJsonAsync<DiscountCodeDto>(cancellationToken: ct);
        return (result, null);
    }

    public async Task<(bool, ErrorResponse?)> DeleteAsync(int discountCodeId, CancellationToken ct = default)
    {
        var response = await _httpClient.DeleteAsync($"api/v1/discount-code/{discountCodeId}", ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: ct);
            return (false, error);
        }

        return (true, null);
    }

    public async Task<(DiscountCodeDto?, ErrorResponse?)> ValidateAsync(int restaurantId, string code, CancellationToken ct = default)
    {
        var request = new ValidateDiscountCodeRequest { Code = code };
        var response = await _httpClient.PostAsJsonAsync(
            $"api/v1/restaurant/{restaurantId}/discount-codes/validate", request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: ct);
            return (null, error);
        }

        var result = await response.Content.ReadFromJsonAsync<DiscountCodeDto>(cancellationToken: ct);
        return (result, null);
    }
}
