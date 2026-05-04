using System.Net.Http.Json;
using DeliverTableClient.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Loyalty;

namespace DeliverTableClient.Services;

public sealed class LoyaltyClientService(HttpClient httpClient) : ILoyaltyClientService
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<(LoyaltyProgramDto?, ErrorResponse?)> GetProgramAsync(int restaurantId, CancellationToken ct = default)
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<LoyaltyProgramDto>(
                ApiRoutes.Loyalty.RestaurantBaseRoute.Replace("{id:int}", restaurantId.ToString()), ct);
            return (result, null);
        }
        catch (Exception ex)
        {
            return (null, new ErrorResponse { Error = ex.Message });
        }
    }

    public async Task<(LoyaltyProgramDto?, ErrorResponse?)> CreateOrUpdateAsync(int restaurantId, CreateLoyaltyProgramRequest request, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            ApiRoutes.Loyalty.RestaurantBaseRoute.Replace("{id:int}", restaurantId.ToString()), request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: ct);
            return (null, error);
        }

        var result = await response.Content.ReadFromJsonAsync<LoyaltyProgramDto>(cancellationToken: ct);
        return (result, null);
    }

    public async Task<(LoyaltyAccountDto?, ErrorResponse?)> GetMyAccountAsync(int restaurantId, CancellationToken ct = default)
    {
        try
        {
            var myAccountUrl = $"{ApiRoutes.Loyalty.RestaurantBaseRoute.Replace("{id:int}", restaurantId.ToString())}/{ApiRoutes.Loyalty.MyAccountRoute}";
            var result = await _httpClient.GetFromJsonAsync<LoyaltyAccountDto>(myAccountUrl, ct);
            return (result, null);
        }
        catch (Exception ex)
        {
            return (null, new ErrorResponse { Error = ex.Message });
        }
    }
}
