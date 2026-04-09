using DeliverTableInfrastructure.Models;
using DeliverTableSharedLibrary.Dtos.Order;

namespace DeliverTableServer.Mappers;

public static class OrderMapper
{
    public static OrderDto ToDto(this Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            RestaurantId = order.RestaurantId,
            RestaurantName = order.Restaurant?.Name ?? string.Empty,
            OrderType = order.OrderType.ToString(),
            Status = order.Status.ToString(),
            PaymentStatus = order.PaymentStatus.ToString(),
            TotalAmount = order.TotalAmount,
            GuestCount = order.GuestCount,
            DeliveryAddress = order.DeliveryAddress,
            Notes = order.Notes,
            ScheduledAt = order.ScheduledAt,
            RestaurantTableId = order.RestaurantTableId,
            IsEventBooking = order.IsEventBooking,
            EventId = order.EventId,
            Items = order.Items.Select(i => i.ToDto()).ToList(),
            CreatedAt = order.CreatedAt,
            OriginalAmount = order.OriginalAmount,
            DiscountAmount = order.DiscountAmount,
            LoyaltyPointsUsed = order.LoyaltyPointsUsed,
            LoyaltyPointsEarned = order.LoyaltyPointsEarned,
            Discounts = order.Discounts.Select(d => d.ToDto()).ToList()
        };
    }

    public static OrderItemDto ToDto(this OrderItem item)
    {
        return new OrderItemDto
        {
            Id = item.Id,
            DishId = item.DishId,
            DishName = item.DishName,
            UnitPrice = item.UnitPrice,
            Quantity = item.Quantity,
            SpecialInstructions = item.SpecialInstructions
        };
    }
}
