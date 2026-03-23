using DeliverTableServer.Common;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Dish;

namespace DeliverTableServer.Services.Interfaces;

public interface IDishService
{
    Task<ServiceResult<PaginatedResult<DishDto>>> GetAllAsync(DishQuery query, CancellationToken ct = default);
    Task<ServiceResult<DishDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<PaginatedResult<DishDto>>> GetByRestaurantIdAsync(DishQuery query, int restaurantId, CancellationToken ct = default);
    Task<ServiceResult<DishDto>> CreateAsync(CreateDishDto dto, int restaurantId, IFormFile? image, CancellationToken ct = default);
    Task<ServiceResult<DishDto>> UpdateAsync(int id, CreateDishDto dto, IFormFile? image, CancellationToken ct = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken ct = default);
}
