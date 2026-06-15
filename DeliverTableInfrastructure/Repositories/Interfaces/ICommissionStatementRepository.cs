using DeliverTableInfrastructure.Models;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableInfrastructure.Repositories.Interfaces;

public interface ICommissionStatementRepository
{
    Task<CommissionStatement> CreateAsync(CommissionStatement statement, CancellationToken ct = default);
    Task UpdateAsync(CommissionStatement statement, CancellationToken ct = default);

    Task<CommissionStatement?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<CommissionStatement?> GetByIdWithLinesAndRecipientAsync(int id, CancellationToken ct = default);

    Task<bool> InvoiceExistsForPeriodAsync(int restaurantId, int year, int month, CancellationToken ct = default);

    Task<List<int>> ListRestaurantIdsWithEligibleOrdersAsync(
        DateTime periodStartUtc, DateTime periodEndUtc, CancellationToken ct = default);

    Task<List<Order>> ListEligibleOrdersForRestaurantAsync(
        int restaurantId, DateTime periodStartUtc, DateTime periodEndUtc, CancellationToken ct = default);

    Task<CommissionStatementLine?> FindLineByRefundEventIdAsync(string refundEventId, CancellationToken ct = default);

    Task<int> AllocateNextNumberAsync(CancellationToken ct = default);

    Task<(List<CommissionStatement> Items, int Total)> AdminListAsync(
        int? year, CommissionStatementKind? kind, int? restaurantId,
        int page, int pageSize, CancellationToken ct = default);
}
