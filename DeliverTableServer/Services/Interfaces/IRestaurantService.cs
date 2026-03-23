using DeliverTableServer.Common;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Restaurant;

namespace DeliverTableServer.Services.Interfaces;

public interface IRestaurantService
{
    Task<ServiceResult<PaginatedResult<RestaurantDto>>> GetAllAsync(RestaurantQuery query, CancellationToken ct = default);
    Task<ServiceResult<PaginatedResult<RestaurantDto>>> GetByOwnerAsync(int ownerId, RestaurantQuery query, CancellationToken ct = default);
    Task<ServiceResult<DetailedRestaurantDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<RestaurantDto>> CreateAsync(CreateRestaurantDto dto, int ownerId, CancellationToken ct = default);
    Task<ServiceResult<DetailedRestaurantDto>> UpdateAsync(int id, UpdateRestaurantDto dto, CancellationToken ct = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken ct = default);
}
