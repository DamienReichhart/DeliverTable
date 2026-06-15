using DeliverTableServer.Common;
using DeliverTableSharedLibrary.Dtos.Admin;
using DeliverTableSharedLibrary.Dtos.Restaurant;

namespace DeliverTableServer.Services.Interfaces;

public interface IRestaurantOrderConfigService
{
    Task<ServiceResult<List<AdminBlockedSlotResponse>>> GetBlockedSlotsAsync(
        int restaurantId,
        int ownerId,
        CancellationToken ct = default);

    Task<ServiceResult<AdminBlockedSlotResponse>> CreateBlockedSlotAsync(
        int restaurantId,
        int ownerId,
        AdminCreateBlockedSlotRequest request,
        CancellationToken ct = default);

    Task<ServiceResult> DeleteBlockedSlotAsync(
        int restaurantId,
        int slotId,
        int ownerId,
        CancellationToken ct = default);

    Task<ServiceResult<TablesCapacityResponse>> GetTablesCapacityAsync(
        int restaurantId,
        int ownerId,
        CancellationToken ct = default);

    Task<ServiceResult<TablesCapacityResponse>> UpdateTablesCapacityAsync(
        int restaurantId,
        int ownerId,
        UpdateTablesCapacityRequest request,
        CancellationToken ct = default);

    Task<ServiceResult<RestaurantOpeningHoursResponse>> GetOpeningHoursAsync(
        int restaurantId,
        int ownerId,
        CancellationToken ct = default);

    Task<ServiceResult<RestaurantOpeningHoursResponse>> UpdateOpeningHoursAsync(
        int restaurantId,
        int ownerId,
        UpdateRestaurantOpeningHoursRequest request,
        CancellationToken ct = default);

    Task<ServiceResult<RestaurantAvailableSlotsResponse>> GetAvailableSlotsAsync(
        int restaurantId,
        RestaurantAvailableSlotsQuery query,
        CancellationToken ct = default);
}
