using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableClient.Services.Interfaces;

public interface IAdminOrderConfigClientService
{
    Task<(List<AdminOrderRuleResponse>? Rules, ErrorResponse? Error)> GetAllRulesAsync(
        CancellationToken ct = default);

    Task<(AdminOrderRuleResponse? Rule, ErrorResponse? Error)> GetRuleByIdAsync(
        int id, CancellationToken ct = default);

    Task<(AdminOrderRuleResponse? Rule, ErrorResponse? Error)> CreateRuleAsync(
        AdminCreateOrderRuleRequest request, CancellationToken ct = default);

    Task<(AdminOrderRuleResponse? Rule, ErrorResponse? Error)> UpdateRuleAsync(
        int id, AdminUpdateOrderRuleRequest request, CancellationToken ct = default);

    Task<(bool Success, ErrorResponse? Error)> DeleteRuleAsync(int id, CancellationToken ct = default);

    Task<(List<AdminBlockedSlotResponse>? Slots, ErrorResponse? Error)> GetAllBlockedSlotsAsync(
        CancellationToken ct = default);

    Task<(AdminBlockedSlotResponse? Slot, ErrorResponse? Error)> GetBlockedSlotByIdAsync(
        int id, CancellationToken ct = default);

    Task<(AdminBlockedSlotResponse? Slot, ErrorResponse? Error)> CreateBlockedSlotAsync(
        AdminCreateBlockedSlotRequest request, CancellationToken ct = default);

    Task<(bool Success, ErrorResponse? Error)> DeleteBlockedSlotAsync(int id, CancellationToken ct = default);
}
