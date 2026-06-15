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
            List<string> queryParams = new List<string>
            {
                $"PageNumber={query.PageNumber}",
                $"PageSize={query.PageSize}"
            };

            string url = $"{ApiRoutes.RestaurantAccount.BaseRoute.Replace("{id:int}", restaurantId.ToString())}?{string.Join("&", queryParams)}";
            RestaurantAccountDto? result = await _httpClient.GetFromJsonAsync<RestaurantAccountDto>(url, ct);
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
        string withdrawUrl = $"{ApiRoutes.RestaurantAccount.BaseRoute.Replace("{id:int}", restaurantId.ToString())}/{ApiRoutes.RestaurantAccount.WithdrawRoute}";
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync(withdrawUrl, request, ct);

        if (!response.IsSuccessStatusCode)
        {
            ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: ct);
            return (null, error);
        }

        RestaurantAccountDto? result = await response.Content.ReadFromJsonAsync<RestaurantAccountDto>(cancellationToken: ct);
        return (result, null);
    }
}
