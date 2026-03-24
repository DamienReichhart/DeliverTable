using System.Net.Http.Json;
using DeliverTableClient.Services.Interfaces;
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
                $"api/v1/restaurant/{restaurantId}/loyalty", ct);
            return (result, null);
        }
        catch (Exception ex)
        {
            return (null, new ErrorResponse { Error = ex.Message });
        }
    }

    public async Task<(LoyaltyProgramDto?, ErrorResponse?)> CreateOrUpdateAsync(int restaurantId, CreateLoyaltyProgramRequest request, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/v1/restaurant/{restaurantId}/loyalty", request, ct);
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
            var result = await _httpClient.GetFromJsonAsync<LoyaltyAccountDto>(
                $"api/v1/restaurant/{restaurantId}/loyalty/my-account", ct);
            return (result, null);
        }
        catch (Exception ex)
        {
            return (null, new ErrorResponse { Error = ex.Message });
        }
    }
}
