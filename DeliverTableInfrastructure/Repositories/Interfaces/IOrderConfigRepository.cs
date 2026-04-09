using DeliverTableInfrastructure.Models;

namespace DeliverTableInfrastructure.Repositories.Interfaces;

/// <summary>
///     Pure data-access abstraction for <see cref="OrderRule"/> and <see cref="OrderBlockedSlot"/> entities.
///     No DTO mapping or business logic -- those belong in the service layer.
/// </summary>
public interface IOrderConfigRepository
{
    // ── OrderRule ──
    Task<List<OrderRule>> GetAllRulesAsync(CancellationToken ct = default);
    Task<OrderRule?> GetRuleByIdAsync(int id, CancellationToken ct = default);
    Task<OrderRule> CreateRuleAsync(OrderRule rule, CancellationToken ct = default);
    Task<OrderRule> UpdateRuleAsync(OrderRule rule, CancellationToken ct = default);
    Task<bool> DeleteRuleAsync(int id, CancellationToken ct = default);

    // ── OrderBlockedSlot ──
    Task<List<OrderBlockedSlot>> GetAllBlockedSlotsAsync(CancellationToken ct = default);
    Task<OrderBlockedSlot?> GetBlockedSlotByIdAsync(int id, CancellationToken ct = default);
    Task<OrderBlockedSlot> CreateBlockedSlotAsync(OrderBlockedSlot slot, CancellationToken ct = default);
    Task<bool> DeleteBlockedSlotAsync(int id, CancellationToken ct = default);
}
