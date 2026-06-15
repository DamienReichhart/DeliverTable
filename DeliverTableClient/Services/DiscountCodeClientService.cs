using System.Net.Http.Json;
using DeliverTableClient.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
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
            List<string> queryParams = new List<string>
            {
                $"PageNumber={query.PageNumber}",
                $"PageSize={query.PageSize}"
            };

            string url = $"{ApiRoutes.DiscountCode.RestaurantBaseRoute.Replace("{id:int}", restaurantId.ToString())}?{string.Join("&", queryParams)}";
            PaginatedResult<DiscountCodeDto>? result = await _httpClient.GetFromJsonAsync<PaginatedResult<DiscountCodeDto>>(url, ct);
            return (result, null);
        }
        catch (Exception ex)
        {
            return (null, new ErrorResponse { Error = ex.Message });
        }
    }

    public async Task<(DiscountCodeDto?, ErrorResponse?)> CreateAsync(int restaurantId, CreateDiscountCodeRequest request, CancellationToken ct = default)
    {
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
            ApiRoutes.DiscountCode.RestaurantBaseRoute.Replace("{id:int}", restaurantId.ToString()), request, ct);
        if (!response.IsSuccessStatusCode)
        {
            ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: ct);
            return (null, error);
        }

        DiscountCodeDto? result = await response.Content.ReadFromJsonAsync<DiscountCodeDto>(cancellationToken: ct);
        return (result, null);
    }

    public async Task<(DiscountCodeDto?, ErrorResponse?)> UpdateAsync(int discountCodeId, UpdateDiscountCodeRequest request, CancellationToken ct = default)
    {
        HttpResponseMessage response = await _httpClient.PutAsJsonAsync($"{ApiRoutes.DiscountCode.Base}/{discountCodeId}", request, ct);
        if (!response.IsSuccessStatusCode)
        {
            ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: ct);
            return (null, error);
        }

        DiscountCodeDto? result = await response.Content.ReadFromJsonAsync<DiscountCodeDto>(cancellationToken: ct);
        return (result, null);
    }

    public async Task<(bool, ErrorResponse?)> DeleteAsync(int discountCodeId, CancellationToken ct = default)
    {
        HttpResponseMessage response = await _httpClient.DeleteAsync($"{ApiRoutes.DiscountCode.Base}/{discountCodeId}", ct);
        if (!response.IsSuccessStatusCode)
        {
            ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: ct);
            return (false, error);
        }

        return (true, null);
    }

    public async Task<(DiscountCodeDto?, ErrorResponse?)> ValidateAsync(int restaurantId, string code, CancellationToken ct = default)
    {
        ValidateDiscountCodeRequest request = new ValidateDiscountCodeRequest { Code = code };
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
            ApiRoutes.DiscountCode.ValidateRoute.Replace("{id:int}", restaurantId.ToString()), request, ct);

        if (!response.IsSuccessStatusCode)
        {
            ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: ct);
            return (null, error);
        }

        DiscountCodeDto? result = await response.Content.ReadFromJsonAsync<DiscountCodeDto>(cancellationToken: ct);
        return (result, null);
    }
}
