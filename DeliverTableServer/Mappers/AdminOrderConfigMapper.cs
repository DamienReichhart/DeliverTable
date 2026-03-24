using DeliverTableServer.Models;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableServer.Mappers;

public static class AdminOrderConfigMapper
{
    public static AdminOrderRuleResponse ToAdminDto(this OrderRule rule)
    {
        return new AdminOrderRuleResponse
        {
            Id = rule.Id,
            RestaurantId = rule.RestaurantId,
            RestaurantName = rule.Restaurant is not null
                ? rule.Restaurant.Name
                : "",
            MinConfirmAmount = rule.MinConfirmAmount,
            MinLeadTimeHours = rule.MinLeadTimeHours,
            MaxAdvanceDays = rule.MaxAdvanceDays,
            SlotDurationMinutes = rule.SlotDurationMinutes,
            AvailabilityRanges = rule.AvailabilityRanges,
            AllowPreorder = rule.AllowPreorder,
            AllowDelivery = rule.AllowDelivery,
            CreatedAt = rule.CreatedAt,
            UpdatedAt = rule.UpdatedAt
        };
    }

    public static AdminBlockedSlotResponse ToAdminDto(this OrderBlockedSlot slot)
    {
        return new AdminBlockedSlotResponse
        {
            Id = slot.Id,
            RestaurantId = slot.RestaurantId,
            RestaurantName = slot.Restaurant is not null
                ? slot.Restaurant.Name
                : "",
            RestaurantTableId = slot.RestaurantTableId,
            StartsAt = slot.StartsAt,
            EndsAt = slot.EndsAt,
            Reason = slot.Reason,
            CreatedAt = slot.CreatedAt
        };
    }
}
