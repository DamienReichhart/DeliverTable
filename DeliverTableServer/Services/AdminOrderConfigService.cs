using DeliverTableServer.Common;
using DeliverTableServer.Constants;
using DeliverTableServer.Mappers;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableServer.Services;

public sealed class AdminOrderConfigService(
    IOrderConfigRepository orderConfigRepository,
    IRestaurantRepository restaurantRepository)
    : IAdminOrderConfigService
{
    private readonly IOrderConfigRepository _orderConfigRepository = orderConfigRepository;
    private readonly IRestaurantRepository _restaurantRepository = restaurantRepository;

    // ── OrderRule ──

    public async Task<ServiceResult<List<AdminOrderRuleResponse>>> GetAllRulesAsync(CancellationToken ct = default)
    {
        List<OrderRule> rules = await _orderConfigRepository.GetAllRulesAsync(ct);
        List<AdminOrderRuleResponse> result = rules.Select(r => r.ToAdminDto()).ToList();
        return result;
    }

    public async Task<ServiceResult<AdminOrderRuleResponse>> GetRuleByIdAsync(int id, CancellationToken ct = default)
    {
        OrderRule? rule = await _orderConfigRepository.GetRuleByIdAsync(id, ct);
        if (rule is null)
            return ServiceError.NotFound(ErrorMessages.OrderRuleNotFound);

        return rule.ToAdminDto();
    }

    public async Task<ServiceResult<AdminOrderRuleResponse>> CreateRuleAsync(
        AdminCreateOrderRuleRequest request, CancellationToken ct = default)
    {
        Restaurant? restaurant = await _restaurantRepository.GetByIdAsync(request.RestaurantId, ct);
        if (restaurant is null)
            return ServiceError.NotFound(ErrorMessages.RestaurantNotFound);

        OrderRule rule = new OrderRule
        {
            RestaurantId = request.RestaurantId,
            MinConfirmAmount = request.MinConfirmAmount,
            MinLeadTimeHours = request.MinLeadTimeHours,
            MaxAdvanceDays = request.MaxAdvanceDays,
            SlotDurationMinutes = request.SlotDurationMinutes,
            TablesCapacityPerSlot = request.TablesCapacityPerSlot,
            AvailabilityRanges = request.AvailabilityRanges,
            AllowPreorder = request.AllowPreorder,
            AllowDelivery = request.AllowDelivery
        };

        OrderRule created = await _orderConfigRepository.CreateRuleAsync(rule, ct);
        return created.ToAdminDto();
    }

    public async Task<ServiceResult<AdminOrderRuleResponse>> UpdateRuleAsync(
        int id, AdminUpdateOrderRuleRequest request, CancellationToken ct = default)
    {
        OrderRule? rule = await _orderConfigRepository.GetRuleByIdAsync(id, ct);
        if (rule is null)
            return ServiceError.NotFound(ErrorMessages.OrderRuleNotFound);

        rule.MinConfirmAmount = request.MinConfirmAmount;
        rule.MinLeadTimeHours = request.MinLeadTimeHours;
        rule.MaxAdvanceDays = request.MaxAdvanceDays;
        rule.SlotDurationMinutes = request.SlotDurationMinutes;
        rule.TablesCapacityPerSlot = request.TablesCapacityPerSlot;
        rule.AvailabilityRanges = request.AvailabilityRanges;
        rule.AllowPreorder = request.AllowPreorder;
        rule.AllowDelivery = request.AllowDelivery;
        rule.UpdatedAt = DateTime.UtcNow;

        OrderRule updated = await _orderConfigRepository.UpdateRuleAsync(rule, ct);
        return updated.ToAdminDto();
    }

    public async Task<ServiceResult> DeleteRuleAsync(int id, CancellationToken ct = default)
    {
        bool deleted = await _orderConfigRepository.DeleteRuleAsync(id, ct);
        if (!deleted)
            return ServiceError.NotFound(ErrorMessages.OrderRuleNotFound);

        return ServiceResult.Success();
    }

    // ── OrderBlockedSlot ──

    public async Task<ServiceResult<List<AdminBlockedSlotResponse>>> GetAllBlockedSlotsAsync(
        CancellationToken ct = default)
    {
        List<OrderBlockedSlot> slots = await _orderConfigRepository.GetAllBlockedSlotsAsync(ct);
        List<AdminBlockedSlotResponse> result = slots.Select(s => s.ToAdminDto()).ToList();
        return result;
    }

    public async Task<ServiceResult<AdminBlockedSlotResponse>> GetBlockedSlotByIdAsync(
        int id, CancellationToken ct = default)
    {
        OrderBlockedSlot? slot = await _orderConfigRepository.GetBlockedSlotByIdAsync(id, ct);
        if (slot is null)
            return ServiceError.NotFound(ErrorMessages.BlockedSlotNotFound);

        return slot.ToAdminDto();
    }

    public async Task<ServiceResult<AdminBlockedSlotResponse>> CreateBlockedSlotAsync(
        AdminCreateBlockedSlotRequest request, CancellationToken ct = default)
    {
        if (request.EndsAt <= request.StartsAt)
            return ServiceError.BadRequest(ErrorMessages.InvalidBlockedSlotDates);

        Restaurant? restaurant = await _restaurantRepository.GetByIdAsync(request.RestaurantId, ct);
        if (restaurant is null)
            return ServiceError.NotFound(ErrorMessages.RestaurantNotFound);

        OrderBlockedSlot slot = new OrderBlockedSlot
        {
            RestaurantId = request.RestaurantId,
            RestaurantTableId = request.RestaurantTableId,
            StartsAt = request.StartsAt,
            EndsAt = request.EndsAt,
            Reason = request.Reason
        };

        OrderBlockedSlot created = await _orderConfigRepository.CreateBlockedSlotAsync(slot, ct);
        return created.ToAdminDto();
    }

    public async Task<ServiceResult> DeleteBlockedSlotAsync(int id, CancellationToken ct = default)
    {
        bool deleted = await _orderConfigRepository.DeleteBlockedSlotAsync(id, ct);
        if (!deleted)
            return ServiceError.NotFound(ErrorMessages.BlockedSlotNotFound);

        return ServiceResult.Success();
    }
}
