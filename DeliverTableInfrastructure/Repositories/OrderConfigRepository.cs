using DeliverTableInfrastructure.Data;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableInfrastructure.Repositories;

public class OrderConfigRepository(DeliverTableContext dbContext) : IOrderConfigRepository
{
    private readonly DeliverTableContext _dbContext = dbContext;

    // ── OrderRule ──

    public async Task<List<OrderRule>> GetAllRulesAsync(CancellationToken ct = default)
        => await _dbContext.OrderRules
            .Include(r => r.Restaurant)
            .OrderBy(r => r.Id)
            .ToListAsync(ct);

    public async Task<OrderRule?> GetRuleByIdAsync(int id, CancellationToken ct = default)
        => await _dbContext.OrderRules
            .Include(r => r.Restaurant)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<OrderRule?> GetRuleByRestaurantIdAsync(int restaurantId, CancellationToken ct = default)
        => await _dbContext.OrderRules
            .Include(r => r.Restaurant)
            .Where(r => r.RestaurantId == restaurantId)
            .OrderByDescending(r => r.UpdatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<OrderRule> CreateRuleAsync(OrderRule rule, CancellationToken ct = default)
    {
        _dbContext.OrderRules.Add(rule);
        await _dbContext.SaveChangesAsync(ct);
        return rule;
    }

    public async Task<OrderRule> UpdateRuleAsync(OrderRule rule, CancellationToken ct = default)
    {
        _dbContext.OrderRules.Update(rule);
        await _dbContext.SaveChangesAsync(ct);
        return rule;
    }

    public async Task<bool> DeleteRuleAsync(int id, CancellationToken ct = default)
    {
        OrderRule? rule = await _dbContext.OrderRules.FindAsync([id], ct);
        if (rule is null) return false;

        _dbContext.OrderRules.Remove(rule);
        await _dbContext.SaveChangesAsync(ct);
        return true;
    }

    // ── OrderBlockedSlot ──

    public async Task<List<OrderBlockedSlot>> GetAllBlockedSlotsAsync(CancellationToken ct = default)
        => await _dbContext.OrderBlockedSlots
            .Include(s => s.Restaurant)
            .OrderBy(s => s.Id)
            .ToListAsync(ct);

    public async Task<List<OrderBlockedSlot>> GetBlockedSlotsByRestaurantAsync(
        int restaurantId, CancellationToken ct = default)
        => await _dbContext.OrderBlockedSlots
            .Include(s => s.Restaurant)
            .Where(s => s.RestaurantId == restaurantId)
            .OrderBy(s => s.StartsAt)
            .ToListAsync(ct);

    public async Task<OrderBlockedSlot?> GetBlockedSlotByIdAsync(int id, CancellationToken ct = default)
        => await _dbContext.OrderBlockedSlots
            .Include(s => s.Restaurant)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<OrderBlockedSlot> CreateBlockedSlotAsync(OrderBlockedSlot slot, CancellationToken ct = default)
    {
        _dbContext.OrderBlockedSlots.Add(slot);
        await _dbContext.SaveChangesAsync(ct);
        return slot;
    }

    public async Task<bool> DeleteBlockedSlotAsync(int id, CancellationToken ct = default)
    {
        OrderBlockedSlot? slot = await _dbContext.OrderBlockedSlots.FindAsync([id], ct);
        if (slot is null) return false;

        _dbContext.OrderBlockedSlots.Remove(slot);
        await _dbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ExistsBlockedSlotOverlapAsync(
        int restaurantId,
        int? restaurantTableId,
        DateTime startsAt,
        DateTime endsAt,
        CancellationToken ct = default)
    {
        // Conflict rules:
        // - a restaurant-wide incoming slot (restaurantTableId == null) conflicts with every slot
        // - an existing restaurant-wide slot conflicts with any incoming slot
        // - two table-specific slots conflict only when they target the same table
        return await _dbContext.OrderBlockedSlots.AnyAsync(s =>
            s.RestaurantId == restaurantId
            && startsAt < s.EndsAt
            && endsAt > s.StartsAt
            && (
                restaurantTableId == null
                || s.RestaurantTableId == null
                || s.RestaurantTableId == restaurantTableId
            ), ct);
    }

    public async Task<bool> IsRestaurantLevelSlotBlockedAsync(
        int restaurantId,
        DateTime startsAt,
        DateTime endsAt,
        CancellationToken ct = default)
    {
        return await _dbContext.OrderBlockedSlots.AnyAsync(s =>
            s.RestaurantId == restaurantId
            && s.RestaurantTableId == null
            && startsAt < s.EndsAt
            && endsAt > s.StartsAt,
            ct);
    }
}
