using System.Net.Http.Json;
using DeliverTableClient.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Event;

namespace DeliverTableClient.Services;

public sealed class RestaurantEventService(HttpClient httpClient) : IRestaurantEventService
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<(List<RestaurantEventResponse>?, ErrorResponse?)> GetByRestaurantAsync(int restaurantId, CancellationToken ct = default)
    {
        try
        {
            var url = ApiRoutes.Event.RestaurantBaseRoute.Replace("{id:int}", restaurantId.ToString());
            var result = await _httpClient.GetFromJsonAsync<List<RestaurantEventResponse>>(url, ct);
            return (result, null);
        }
        catch (Exception ex)
        {
            return (null, new ErrorResponse { Error = ex.Message });
        }
    }

    public async Task<(RestaurantEventResponse?, ErrorResponse?)> CreateAsync(int restaurantId, CreateRestaurantEventRequest request, CancellationToken ct = default)
    {
        var url = ApiRoutes.Event.RestaurantBaseRoute.Replace("{id:int}", restaurantId.ToString());
        var response = await _httpClient.PostAsJsonAsync(url, request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: ct);
            return (null, error);
        }

        var result = await response.Content.ReadFromJsonAsync<RestaurantEventResponse>(cancellationToken: ct);
        return (result, null);
    }

    public async Task<(RestaurantEventResponse?, ErrorResponse?)> UpdateAsync(int eventId, UpdateRestaurantEventRequest request, CancellationToken ct = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"{ApiRoutes.Event.Base}/{eventId}", request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: ct);
            return (null, error);
        }

        var result = await response.Content.ReadFromJsonAsync<RestaurantEventResponse>(cancellationToken: ct);
        return (result, null);
    }

    public async Task<(bool, ErrorResponse?)> DeleteAsync(int eventId, CancellationToken ct = default)
    {
        var response = await _httpClient.DeleteAsync($"{ApiRoutes.Event.Base}/{eventId}", ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: ct);
            return (false, error);
        }

        return (true, null);
    }

    public async Task<(List<RestaurantEventResponse>?, ErrorResponse?)> GetActiveByRestaurantAsync(int restaurantId, CancellationToken ct = default)
    {
        try
        {
            var url = ApiRoutes.Event.ActiveRoute.Replace("{id:int}", restaurantId.ToString());
            var result = await _httpClient.GetFromJsonAsync<List<RestaurantEventResponse>>(url, ct);
            return (result, null);
        }
        catch (Exception ex)
        {
            return (null, new ErrorResponse { Error = ex.Message });
        }
    }
}
