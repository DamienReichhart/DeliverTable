using DeliverTableInfrastructure.Data;
using DeliverTableInfrastructure.Extensions;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableSharedLibrary.Enums;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableInfrastructure.Repositories;

public class CommissionStatementRepository(DeliverTableContext dbContext) : ICommissionStatementRepository
{
    private readonly DeliverTableContext _dbContext = dbContext;

    public async Task<CommissionStatement> CreateAsync(CommissionStatement statement, CancellationToken ct = default)
    {
        _dbContext.CommissionStatements.Add(statement);
        await _dbContext.SaveChangesAsync(ct);
        return statement;
    }

    public async Task UpdateAsync(CommissionStatement statement, CancellationToken ct = default)
    {
        statement.UpdatedAt = DateTime.UtcNow;
        _dbContext.CommissionStatements.Update(statement);
        await _dbContext.SaveChangesAsync(ct);
    }

    public Task<CommissionStatement?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _dbContext.CommissionStatements.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<CommissionStatement?> GetByIdWithLinesAndRecipientAsync(int id, CancellationToken ct = default) =>
        _dbContext.CommissionStatements
            .Include(s => s.Lines)
            .Include(s => s.RecipientRestaurant)
                .ThenInclude(r => r.Owner)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<bool> InvoiceExistsForPeriodAsync(int restaurantId, int year, int month, CancellationToken ct = default) =>
        _dbContext.CommissionStatements.AnyAsync(
            s => s.RecipientRestaurantId == restaurantId
              && s.PeriodYear == year
              && s.PeriodMonth == month
              && s.Kind == CommissionStatementKind.Invoice,
            ct);

    public Task<List<int>> ListRestaurantIdsWithEligibleOrdersAsync(
        DateTime periodStartUtc, DateTime periodEndUtc, CancellationToken ct = default) =>
        _dbContext.Orders
            .Where(o => o.Status == DeliverTableSharedLibrary.Enums.OrderStatus.Delivered
                     && o.PaymentStatus == DeliverTableSharedLibrary.Enums.PaymentStatus.Completed
                     && o.DeliveredAt != null
                     && o.DeliveredAt >= periodStartUtc
                     && o.DeliveredAt < periodEndUtc
                     && o.CommissionStatementId == null)
            .Select(o => o.RestaurantId)
            .Distinct()
            .ToListAsync(ct);

    public Task<List<Order>> ListEligibleOrdersForRestaurantAsync(
        int restaurantId, DateTime periodStartUtc, DateTime periodEndUtc, CancellationToken ct = default) =>
        _dbContext.Orders
            .Include(o => o.Payments).ThenInclude(p => p.Refunds)
            .Where(o => o.RestaurantId == restaurantId
                     && o.Status == DeliverTableSharedLibrary.Enums.OrderStatus.Delivered
                     && o.PaymentStatus == DeliverTableSharedLibrary.Enums.PaymentStatus.Completed
                     && o.DeliveredAt != null
                     && o.DeliveredAt >= periodStartUtc
                     && o.DeliveredAt < periodEndUtc
                     && o.CommissionStatementId == null)
            .OrderBy(o => o.DeliveredAt)
            .ToListAsync(ct);

    public Task<CommissionStatementLine?> FindLineByRefundEventIdAsync(string refundEventId, CancellationToken ct = default) =>
        _dbContext.CommissionStatementLines.FirstOrDefaultAsync(l => l.RefundEventId == refundEventId, ct);

    public async Task<(List<CommissionStatement> Items, int Total)> AdminListAsync(
        int? year, CommissionStatementKind? kind, int? restaurantId,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _dbContext.CommissionStatements
            .Include(s => s.RecipientRestaurant)
            .AsQueryable();

        if (year.HasValue) query = query.Where(s => s.PeriodYear == year.Value);
        if (kind.HasValue) query = query.Where(s => s.Kind == kind.Value);
        if (restaurantId.HasValue) query = query.Where(s => s.RecipientRestaurantId == restaurantId.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(s => s.IssuedAt)
            .Paginate(page, pageSize)
            .ToListAsync(ct);
        return (items, total);
    }

    public async Task<int> AllocateNextNumberAsync(CancellationToken ct = default)
    {
        const int maxAttempts = 5;
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var counter = await _dbContext.CommissionStatementCounters
                    .FirstOrDefaultAsync(c => c.Id == 1, ct)
                    ?? throw new InvalidOperationException("CommissionStatementCounter row missing — migration seed not applied.");
                var n = counter.NextNumber;
                counter.NextNumber = n + 1;
                await _dbContext.SaveChangesAsync(ct);
                return n;
            }
            catch (DbUpdateConcurrencyException) when (attempt < maxAttempts)
            {
                _dbContext.ChangeTracker.Clear();
                await Task.Delay(10 * attempt, ct);
            }
        }
        throw new InvalidOperationException("Failed to allocate commission statement number after retries.");
    }
}
