using System.Net.Http.Json;
using DeliverTableClient.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Admin;
using DeliverTableSharedLibrary.Dtos.Restaurant;

namespace DeliverTableClient.Services;

public sealed class RestaurantOrderConfigClientService(HttpClient httpClient) : IRestaurantOrderConfigClientService
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<(List<AdminBlockedSlotResponse>? Slots, ErrorResponse? Error)> GetBlockedSlotsAsync(
        int restaurantId,
        CancellationToken ct = default)
    {
        string url = ApiRoutes.OrderConfig.RestaurantBlockedSlotsRoute.Replace("{id:int}", restaurantId.ToString());

        using HttpResponseMessage response = await _httpClient.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        List<AdminBlockedSlotResponse>? items = await response.Content.ReadFromJsonAsync<List<AdminBlockedSlotResponse>>(cancellationToken: ct);
        return items is not null
            ? (items, null)
            : (null, new ErrorResponse { Error = "Impossible de lire la liste des créneaux bloqués", Status = (int)response.StatusCode });
    }

    public async Task<(AdminBlockedSlotResponse? Slot, ErrorResponse? Error)> CreateBlockedSlotAsync(
        int restaurantId,
        AdminCreateBlockedSlotRequest request,
        CancellationToken ct = default)
    {
        string url = ApiRoutes.OrderConfig.RestaurantBlockedSlotsRoute.Replace("{id:int}", restaurantId.ToString());

        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(url, request, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        AdminBlockedSlotResponse? item = await response.Content.ReadFromJsonAsync<AdminBlockedSlotResponse>(cancellationToken: ct);
        return (item, null);
    }

    public async Task<(bool Success, ErrorResponse? Error)> DeleteBlockedSlotAsync(
        int restaurantId,
        int slotId,
        CancellationToken ct = default)
    {
        string url = ApiRoutes.OrderConfig.RestaurantBlockedSlotByIdRoute
            .Replace("{id:int}", restaurantId.ToString())
            .Replace("{slotId:int}", slotId.ToString());

        using HttpResponseMessage response = await _httpClient.DeleteAsync(url, ct);
        if (!response.IsSuccessStatusCode)
            return (false, await ReadError(response, ct));

        return (true, null);
    }

    public async Task<(TablesCapacityResponse? Capacity, ErrorResponse? Error)> GetTablesCapacityAsync(
        int restaurantId,
        CancellationToken ct = default)
    {
        string url = ApiRoutes.OrderConfig.TablesCapacityRoute.Replace("{id:int}", restaurantId.ToString());

        using HttpResponseMessage response = await _httpClient.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        TablesCapacityResponse? item = await response.Content.ReadFromJsonAsync<TablesCapacityResponse>(cancellationToken: ct);
        return (item, null);
    }

    public async Task<(TablesCapacityResponse? Capacity, ErrorResponse? Error)> UpdateTablesCapacityAsync(
        int restaurantId,
        UpdateTablesCapacityRequest request,
        CancellationToken ct = default)
    {
        string url = ApiRoutes.OrderConfig.TablesCapacityRoute.Replace("{id:int}", restaurantId.ToString());

        using HttpResponseMessage response = await _httpClient.PutAsJsonAsync(url, request, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        TablesCapacityResponse? item = await response.Content.ReadFromJsonAsync<TablesCapacityResponse>(cancellationToken: ct);
        return (item, null);
    }

    public async Task<(RestaurantOpeningHoursResponse? OpeningHours, ErrorResponse? Error)> GetOpeningHoursAsync(
        int restaurantId,
        CancellationToken ct = default)
    {
        string url = ApiRoutes.OrderConfig.OpeningHoursRoute.Replace("{id:int}", restaurantId.ToString());

        using HttpResponseMessage response = await _httpClient.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        RestaurantOpeningHoursResponse? item = await response.Content.ReadFromJsonAsync<RestaurantOpeningHoursResponse>(cancellationToken: ct);
        return (item, null);
    }

    public async Task<(RestaurantOpeningHoursResponse? OpeningHours, ErrorResponse? Error)> UpdateOpeningHoursAsync(
        int restaurantId,
        UpdateRestaurantOpeningHoursRequest request,
        CancellationToken ct = default)
    {
        string url = ApiRoutes.OrderConfig.OpeningHoursRoute.Replace("{id:int}", restaurantId.ToString());

        using HttpResponseMessage response = await _httpClient.PutAsJsonAsync(url, request, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        RestaurantOpeningHoursResponse? item = await response.Content.ReadFromJsonAsync<RestaurantOpeningHoursResponse>(cancellationToken: ct);
        return (item, null);
    }

    public async Task<(RestaurantAvailableSlotsResponse? Slots, ErrorResponse? Error)> GetAvailableSlotsAsync(
        int restaurantId,
        RestaurantAvailableSlotsQuery query,
        CancellationToken ct = default)
    {
        string queryString = $"date={query.Date:yyyy-MM-dd}&guestCount={query.GuestCount}";
        string url = ApiRoutes.OrderConfig.AvailableSlotsRoute
            .Replace("{id:int}", restaurantId.ToString())
            + $"?{queryString}";

        using HttpResponseMessage response = await _httpClient.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response, ct));

        RestaurantAvailableSlotsResponse? item = await response.Content.ReadFromJsonAsync<RestaurantAvailableSlotsResponse>(cancellationToken: ct);
        return (item, null);
    }

    private static async Task<ErrorResponse> ReadError(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: ct);
            return error ?? new ErrorResponse { Error = "Une erreur est survenue", Status = (int)response.StatusCode };
        }
        catch
        {
            return new ErrorResponse { Error = "Une erreur est survenue", Status = (int)response.StatusCode };
        }
    }
}