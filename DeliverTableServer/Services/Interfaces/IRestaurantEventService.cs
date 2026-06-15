using DeliverTableServer.Common;
using DeliverTableSharedLibrary.Dtos.Event;

namespace DeliverTableServer.Services.Interfaces;

public interface IRestaurantEventService
{
    Task<ServiceResult<List<RestaurantEventResponse>>> GetByRestaurantAsync(
        int restaurantId, int ownerId, CancellationToken ct = default);

    Task<ServiceResult<RestaurantEventResponse>> GetByIdAsync(
        int eventId, int ownerId, CancellationToken ct = default);

    Task<ServiceResult<RestaurantEventResponse>> CreateAsync(
        int restaurantId, int ownerId, CreateRestaurantEventRequest request, CancellationToken ct = default);

    Task<ServiceResult<RestaurantEventResponse>> UpdateAsync(
        int eventId, int ownerId, UpdateRestaurantEventRequest request, CancellationToken ct = default);

    Task<ServiceResult> DeleteAsync(int eventId, int ownerId, CancellationToken ct = default);

    Task<ServiceResult<List<RestaurantEventResponse>>> GetActiveByRestaurantAsync(
        int restaurantId, CancellationToken ct = default);
}
