using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Event;

namespace DeliverTableClient.Services.Interfaces;

public interface IRestaurantEventService
{
    Task<(List<RestaurantEventResponse>?, ErrorResponse?)> GetByRestaurantAsync(int restaurantId, CancellationToken ct = default);
    Task<(RestaurantEventResponse?, ErrorResponse?)> CreateAsync(int restaurantId, CreateRestaurantEventRequest request, CancellationToken ct = default);
    Task<(RestaurantEventResponse?, ErrorResponse?)> UpdateAsync(int eventId, UpdateRestaurantEventRequest request, CancellationToken ct = default);
    Task<(bool, ErrorResponse?)> DeleteAsync(int eventId, CancellationToken ct = default);
    Task<(List<RestaurantEventResponse>?, ErrorResponse?)> GetActiveByRestaurantAsync(int restaurantId, CancellationToken ct = default);
}
