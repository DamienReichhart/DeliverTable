using DeliverTableServer.Common;
using DeliverTableServer.Constants;
using DeliverTableServer.Mappers;
using DeliverTableServer.Models;
using DeliverTableServer.Repositories.Interfaces;
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
        var rules = await _orderConfigRepository.GetAllRulesAsync(ct);
        var result = rules.Select(r => r.ToAdminDto()).ToList();
        return result;
    }

    public async Task<ServiceResult<AdminOrderRuleResponse>> GetRuleByIdAsync(int id, CancellationToken ct = default)
    {
        var rule = await _orderConfigRepository.GetRuleByIdAsync(id, ct);
        if (rule is null)
            return new ServiceError(ErrorMessages.OrderRuleNotFound, 404);

        return rule.ToAdminDto();
    }

    public async Task<ServiceResult<AdminOrderRuleResponse>> CreateRuleAsync(
        AdminCreateOrderRuleRequest request, CancellationToken ct = default)
    {
        var restaurant = await _restaurantRepository.GetByIdAsync(request.RestaurantId, ct);
        if (restaurant is null)
            return new ServiceError(ErrorMessages.RestaurantNotFound, 404);

        var rule = new OrderRule
        {
            RestaurantId = request.RestaurantId,
            MinConfirmAmount = request.MinConfirmAmount,
            MinLeadTimeHours = request.MinLeadTimeHours,
            MaxAdvanceDays = request.MaxAdvanceDays,
            SlotDurationMinutes = request.SlotDurationMinutes,
            AvailabilityRanges = request.AvailabilityRanges,
            AllowPreorder = request.AllowPreorder,
            AllowDelivery = request.AllowDelivery
        };

        var created = await _orderConfigRepository.CreateRuleAsync(rule, ct);
        return created.ToAdminDto();
    }

    public async Task<ServiceResult<AdminOrderRuleResponse>> UpdateRuleAsync(
        int id, AdminUpdateOrderRuleRequest request, CancellationToken ct = default)
    {
        var rule = await _orderConfigRepository.GetRuleByIdAsync(id, ct);
        if (rule is null)
            return new ServiceError(ErrorMessages.OrderRuleNotFound, 404);

        rule.MinConfirmAmount = request.MinConfirmAmount;
        rule.MinLeadTimeHours = request.MinLeadTimeHours;
        rule.MaxAdvanceDays = request.MaxAdvanceDays;
        rule.SlotDurationMinutes = request.SlotDurationMinutes;
        rule.AvailabilityRanges = request.AvailabilityRanges;
        rule.AllowPreorder = request.AllowPreorder;
        rule.AllowDelivery = request.AllowDelivery;
        rule.UpdatedAt = DateTime.UtcNow;

        var updated = await _orderConfigRepository.UpdateRuleAsync(rule, ct);
        return updated.ToAdminDto();
    }

    public async Task<ServiceResult> DeleteRuleAsync(int id, CancellationToken ct = default)
    {
        var deleted = await _orderConfigRepository.DeleteRuleAsync(id, ct);
        if (!deleted)
            return new ServiceError(ErrorMessages.OrderRuleNotFound, 404);

        return ServiceResult.Success();
    }

    // ── OrderBlockedSlot ──

    public async Task<ServiceResult<List<AdminBlockedSlotResponse>>> GetAllBlockedSlotsAsync(
        CancellationToken ct = default)
    {
        var slots = await _orderConfigRepository.GetAllBlockedSlotsAsync(ct);
        var result = slots.Select(s => s.ToAdminDto()).ToList();
        return result;
    }

    public async Task<ServiceResult<AdminBlockedSlotResponse>> GetBlockedSlotByIdAsync(
        int id, CancellationToken ct = default)
    {
        var slot = await _orderConfigRepository.GetBlockedSlotByIdAsync(id, ct);
        if (slot is null)
            return new ServiceError(ErrorMessages.BlockedSlotNotFound, 404);

        return slot.ToAdminDto();
    }

    public async Task<ServiceResult<AdminBlockedSlotResponse>> CreateBlockedSlotAsync(
        AdminCreateBlockedSlotRequest request, CancellationToken ct = default)
    {
        if (request.EndsAt <= request.StartsAt)
            return new ServiceError(ErrorMessages.InvalidBlockedSlotDates, 400);

        var restaurant = await _restaurantRepository.GetByIdAsync(request.RestaurantId, ct);
        if (restaurant is null)
            return new ServiceError(ErrorMessages.RestaurantNotFound, 404);

        var slot = new OrderBlockedSlot
        {
            RestaurantId = request.RestaurantId,
            RestaurantTableId = request.RestaurantTableId,
            StartsAt = request.StartsAt,
            EndsAt = request.EndsAt,
            Reason = request.Reason
        };

        var created = await _orderConfigRepository.CreateBlockedSlotAsync(slot, ct);
        return created.ToAdminDto();
    }

    public async Task<ServiceResult> DeleteBlockedSlotAsync(int id, CancellationToken ct = default)
    {
        var deleted = await _orderConfigRepository.DeleteBlockedSlotAsync(id, ct);
        if (!deleted)
            return new ServiceError(ErrorMessages.BlockedSlotNotFound, 404);

        return ServiceResult.Success();
    }
}
