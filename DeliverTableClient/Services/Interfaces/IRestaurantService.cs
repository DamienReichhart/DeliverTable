using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Restaurant;

namespace DeliverTableClient.Services.Interfaces;

public interface IRestaurantService
{
    Task<bool> CreateRestaurant(CreateRestaurantDto creationDto, CancellationToken cancellationToken = default);
    Task<(PaginatedResult<RestaurantDto>?, ErrorResponse?)> GetConnectedUserRestaurants(CancellationToken cancellationToken = default);
    Task<bool> DeleteRestaurant(int id);
    Task<(DetailedRestaurantDto?, ErrorResponse?)> GetRestaurantById(int id, CancellationToken cancellationToken = default);
    Task<(DetailedRestaurantDto? dto, ErrorResponse? error)> UpdateRestaurant(UpdateRestaurantDto updateDto, int id, CancellationToken cancellationToken = default);
}
