using DeliverTableServer.Models;
using DeliverTableSharedLibrary.Dtos.Dish;

namespace DeliverTableServer.Repositories.Interfaces;

/// <summary>
///     Pure data-access abstraction for <see cref="Dish"/> entities.
///     No DTO mapping, image handling, or business logic -- those belong in the service layer.
/// </summary>
public interface IDishRepository
{
    Task<(List<Dish> Items, int TotalCount)> GetAllAsync(DishQuery query, CancellationToken ct = default);
    Task<(List<Dish> Items, int TotalCount)> GetByRestaurantIdAsync(DishQuery query, int restaurantId, CancellationToken ct = default);
    Task<Dish?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Dish> CreateAsync(Dish dish, CancellationToken ct = default);
    Task<Dish> UpdateAsync(Dish dish, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
