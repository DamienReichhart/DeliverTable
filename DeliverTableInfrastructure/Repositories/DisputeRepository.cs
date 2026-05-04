using DeliverTableInfrastructure.Data;
using DeliverTableInfrastructure.Extensions;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableSharedLibrary.Enums;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableInfrastructure.Repositories;

public class DisputeRepository(DeliverTableContext dbContext) : IDisputeRepository
{
    private readonly DeliverTableContext _dbContext = dbContext;

    public async Task<Dispute> CreateAsync(Dispute dispute, CancellationToken ct = default)
    {
        _dbContext.Disputes.Add(dispute);
        await _dbContext.SaveChangesAsync(ct);
        return dispute;
    }

    public async Task UpdateAsync(Dispute dispute, CancellationToken ct = default)
    {
        dispute.UpdatedAt = DateTime.UtcNow;
        _dbContext.Disputes.Update(dispute);
        await _dbContext.SaveChangesAsync(ct);
    }

    public Task<Dispute?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _dbContext.Disputes
            .Include(d => d.Restaurant)
            .Include(d => d.Order).ThenInclude(o => o.Customer)
            .Include(d => d.Payment)
            .FirstOrDefaultAsync(d => d.Id == id, ct);

    public Task<Dispute?> GetByStripeDisputeIdAsync(string stripeDisputeId, CancellationToken ct = default) =>
        _dbContext.Disputes.FirstOrDefaultAsync(d => d.StripeDisputeId == stripeDisputeId, ct);

    public Task<bool> HasOpenForOrderAsync(int orderId, CancellationToken ct = default) =>
        _dbContext.Disputes.AnyAsync(d => d.OrderId == orderId && d.State == DisputeState.Open, ct);

    public async Task<(List<Dispute> Items, int Total)> ListForRestaurantAsync(
        int restaurantId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _dbContext.Disputes.Where(d => d.RestaurantId == restaurantId);
        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(d => d.OpenedAt)
            .Paginate(page, pageSize)
            .ToListAsync(ct);
        return (items, total);
    }

    public async Task<(List<Dispute> Items, int Total)> AdminListAsync(
        DisputeState? state,
        int? restaurantId,
        int? orderId,
        int? year,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _dbContext.Disputes
            .Include(d => d.Restaurant)
            .Include(d => d.Order).ThenInclude(o => o.Customer)
            .AsQueryable();

        if (state.HasValue)
            query = query.Where(d => d.State == state.Value);
        if (restaurantId.HasValue)
            query = query.Where(d => d.RestaurantId == restaurantId.Value);
        if (orderId.HasValue)
            query = query.Where(d => d.OrderId == orderId.Value);
        if (year.HasValue)
            query = query.Where(d => d.OpenedAt.Year == year.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(d => d.OpenedAt)
            .Paginate(page, pageSize)
            .ToListAsync(ct);
        return (items, total);
    }
}
