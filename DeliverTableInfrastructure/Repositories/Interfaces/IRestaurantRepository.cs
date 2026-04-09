using DeliverTableInfrastructure.Models;
using DeliverTableSharedLibrary.Dtos.Restaurant;

namespace DeliverTableInfrastructure.Repositories.Interfaces;

/// <summary>
///     Pure data-access abstraction for <see cref="Restaurant"/> entities.
///     No DTO mapping or business logic -- those belong in the service layer.
/// </summary>
public interface IRestaurantRepository
{
    Task<Restaurant> CreateAsync(Restaurant restaurant, CancellationToken ct = default);
    Task<(List<Restaurant> Items, int TotalCount)> GetAllAsync(RestaurantQuery query, CancellationToken ct = default);
    Task<Restaurant?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    Task<Restaurant> UpdateAsync(Restaurant restaurant, CancellationToken ct = default);
    Task<(List<Restaurant> Items, int TotalCount)> GetByOwnerAsync(int ownerId, RestaurantQuery query, CancellationToken ct = default);
    Task<List<Restaurant>> GetForMapAsync(RestaurantQuery query, CancellationToken ct = default);
    Task<List<Restaurant>> GetAllUnscopedAsync(CancellationToken ct = default);
    Task<Restaurant?> GetByIdWithOwnerAsync(int id, CancellationToken ct = default);
}
