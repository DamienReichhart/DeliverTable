using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Admin;
using DeliverTableSharedLibrary.Dtos.Restaurant;

namespace DeliverTableClient.Services.Interfaces;

public interface IRestaurantOrderConfigClientService
{
    Task<(List<AdminBlockedSlotResponse>? Slots, ErrorResponse? Error)> GetBlockedSlotsAsync(
        int restaurantId,
        CancellationToken ct = default);

    Task<(AdminBlockedSlotResponse? Slot, ErrorResponse? Error)> CreateBlockedSlotAsync(
        int restaurantId,
        AdminCreateBlockedSlotRequest request,
        CancellationToken ct = default);

    Task<(bool Success, ErrorResponse? Error)> DeleteBlockedSlotAsync(
        int restaurantId,
        int slotId,
        CancellationToken ct = default);

    Task<(TablesCapacityResponse? Capacity, ErrorResponse? Error)> GetTablesCapacityAsync(
        int restaurantId,
        CancellationToken ct = default);

    Task<(TablesCapacityResponse? Capacity, ErrorResponse? Error)> UpdateTablesCapacityAsync(
        int restaurantId,
        UpdateTablesCapacityRequest request,
        CancellationToken ct = default);

    Task<(RestaurantOpeningHoursResponse? OpeningHours, ErrorResponse? Error)> GetOpeningHoursAsync(
        int restaurantId,
        CancellationToken ct = default);

    Task<(RestaurantOpeningHoursResponse? OpeningHours, ErrorResponse? Error)> UpdateOpeningHoursAsync(
        int restaurantId,
        UpdateRestaurantOpeningHoursRequest request,
        CancellationToken ct = default);

    Task<(RestaurantAvailableSlotsResponse? Slots, ErrorResponse? Error)> GetAvailableSlotsAsync(
        int restaurantId,
        RestaurantAvailableSlotsQuery query,
        CancellationToken ct = default);
}