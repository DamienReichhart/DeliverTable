using System.Net.Http.Json;
using DeliverTableClient.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.RestaurantAccount;

namespace DeliverTableClient.Services;

public sealed class RestaurantAccountService(HttpClient httpClient) : IRestaurantAccountService
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<(RestaurantAccountDto?, ErrorResponse?)> GetAccountAsync(
        int restaurantId, TransactionQuery query, CancellationToken ct = default)
    {
        try
        {
            var queryParams = new List<string>
            {
                $"PageNumber={query.PageNumber}",
                $"PageSize={query.PageSize}"
            };

            var url = $"api/v1/restaurant/{restaurantId}/account?{string.Join("&", queryParams)}";
            var result = await _httpClient.GetFromJsonAsync<RestaurantAccountDto>(url, ct);
            return (result, null);
        }
        catch (Exception ex)
        {
            return (null, new ErrorResponse { Error = ex.Message });
        }
    }

    public async Task<(RestaurantAccountDto?, ErrorResponse?)> WithdrawAsync(
        int restaurantId, WithdrawRequest request, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"api/v1/restaurant/{restaurantId}/account/withdraw", request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: ct);
            return (null, error);
        }

        var result = await response.Content.ReadFromJsonAsync<RestaurantAccountDto>(cancellationToken: ct);
        return (result, null);
    }
}
