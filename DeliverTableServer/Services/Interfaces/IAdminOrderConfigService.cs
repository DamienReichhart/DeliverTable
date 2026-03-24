using DeliverTableServer.Common;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableServer.Services.Interfaces;

public interface IAdminOrderConfigService
{
    // ── OrderRule ──
    Task<ServiceResult<List<AdminOrderRuleResponse>>> GetAllRulesAsync(CancellationToken ct = default);
    Task<ServiceResult<AdminOrderRuleResponse>> GetRuleByIdAsync(int id, CancellationToken ct = default);

    Task<ServiceResult<AdminOrderRuleResponse>> CreateRuleAsync(
        AdminCreateOrderRuleRequest request, CancellationToken ct = default);

    Task<ServiceResult<AdminOrderRuleResponse>> UpdateRuleAsync(
        int id, AdminUpdateOrderRuleRequest request, CancellationToken ct = default);

    Task<ServiceResult> DeleteRuleAsync(int id, CancellationToken ct = default);

    // ── OrderBlockedSlot ──
    Task<ServiceResult<List<AdminBlockedSlotResponse>>> GetAllBlockedSlotsAsync(CancellationToken ct = default);
    Task<ServiceResult<AdminBlockedSlotResponse>> GetBlockedSlotByIdAsync(int id, CancellationToken ct = default);

    Task<ServiceResult<AdminBlockedSlotResponse>> CreateBlockedSlotAsync(
        AdminCreateBlockedSlotRequest request, CancellationToken ct = default);

    Task<ServiceResult> DeleteBlockedSlotAsync(int id, CancellationToken ct = default);
}
