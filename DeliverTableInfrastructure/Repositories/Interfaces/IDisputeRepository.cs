using DeliverTableInfrastructure.Models;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableInfrastructure.Repositories.Interfaces;

public interface IDisputeRepository
{
    Task<Dispute> CreateAsync(Dispute dispute, CancellationToken ct = default);
    Task UpdateAsync(Dispute dispute, CancellationToken ct = default);
    Task<Dispute?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Dispute?> GetByStripeDisputeIdAsync(string stripeDisputeId, CancellationToken ct = default);
    Task<bool> HasOpenForOrderAsync(int orderId, CancellationToken ct = default);
    Task<(List<Dispute> Items, int Total)> ListForRestaurantAsync(int restaurantId, int page, int pageSize, CancellationToken ct = default);
    Task<(List<Dispute> Items, int Total)> AdminListAsync(
        DisputeState? state,
        int? restaurantId,
        int? orderId,
        int? year,
        int page,
        int pageSize,
        CancellationToken ct = default);
}
