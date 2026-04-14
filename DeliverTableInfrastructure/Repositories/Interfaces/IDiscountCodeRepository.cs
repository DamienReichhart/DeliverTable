using DeliverTableInfrastructure.Models;
using DeliverTableSharedLibrary.Dtos.DiscountCode;

namespace DeliverTableInfrastructure.Repositories.Interfaces;

public interface IDiscountCodeRepository
{
    Task<Models.DiscountCode> CreateAsync(Models.DiscountCode code, CancellationToken ct = default);
    Task<Models.DiscountCode?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Models.DiscountCode?> GetByCodeAndRestaurantAsync(string code, int restaurantId, CancellationToken ct = default);
    Task<(List<Models.DiscountCode> Items, int TotalCount)> GetByRestaurantAsync(int restaurantId, DiscountCodeQuery query, CancellationToken ct = default);
    Task<int> GetRedemptionCountByUserAsync(int discountCodeId, int customerId, CancellationToken ct = default);
    Task<Models.DiscountCode> UpdateAsync(Models.DiscountCode code, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    Task<DiscountCodeRedemption> CreateRedemptionAsync(DiscountCodeRedemption redemption, CancellationToken ct = default);
    Task<List<Models.DiscountCode>> GetAllUnscopedAsync(CancellationToken ct = default);
    Task<Models.DiscountCode?> GetByIdWithRestaurantAsync(int id, CancellationToken ct = default);
    Task<List<DiscountCodeRedemption>> GetRedemptionsByCodeIdAsync(int discountCodeId, CancellationToken ct = default);

    Task MarkPendingRedemptionsCommittedForOrderAsync(int orderId, CancellationToken ct = default);
    Task MarkPendingRedemptionsReversedForOrderAsync(int orderId, CancellationToken ct = default);
}
