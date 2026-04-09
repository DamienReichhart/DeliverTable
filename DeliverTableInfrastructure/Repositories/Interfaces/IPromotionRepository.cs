using DeliverTableInfrastructure.Models;
using DeliverTableSharedLibrary.Dtos.Promotion;

namespace DeliverTableInfrastructure.Repositories.Interfaces;

public interface IPromotionRepository
{
    Task<Promotion> CreateAsync(Promotion promotion, CancellationToken ct = default);
    Task<Promotion?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<(List<Promotion> Items, int TotalCount)> GetByRestaurantAsync(int restaurantId, PromotionQuery query, CancellationToken ct = default);
    Task<List<Promotion>> GetActiveByRestaurantAsync(int restaurantId, CancellationToken ct = default);
    Task<Promotion> UpdateAsync(Promotion promotion, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    Task<List<Promotion>> GetAllUnscopedAsync(CancellationToken ct = default);
    Task<Promotion?> GetByIdWithRestaurantAsync(int id, CancellationToken ct = default);
}
