using DeliverTableInfrastructure.Extensions;
using DeliverTableInfrastructure.Models;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableServer.Mappers;

public static class AdminOrderMapper
{
    public static AdminOrderResponse ToAdminDto(this Order order)
    {
        return new AdminOrderResponse
        {
            Id = order.Id,
            OrderType = order.OrderType.ToString(),
            Status = order.Status.ToString(),
            PaymentStatus = order.PaymentStatus.ToString(),
            TotalAmount = order.TotalAmount,
            OriginalAmount = order.OriginalAmount,
            DiscountAmount = order.DiscountAmount,
            LoyaltyPointsUsed = order.LoyaltyPointsUsed,
            LoyaltyPointsEarned = order.LoyaltyPointsEarned,
            GuestCount = order.GuestCount,
            DeliveryAddress = order.DeliveryAddress,
            Notes = order.Notes,
            ScheduledAt = order.ScheduledAt,
            IsEventBooking = order.IsEventBooking,
            Source = order.Source.ToString(),
            CustomerId = order.CustomerId,
            CustomerName = order.Customer?.GetFullName() ?? "",
            RestaurantId = order.RestaurantId,
            RestaurantName = order.Restaurant is not null
                ? order.Restaurant.Name
                : "",
            Items = order.Items.Select(i => i.ToAdminDto()).ToList(),
            Payments = order.Payments.Select(p => p.ToAdminDto()).ToList(),
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt
        };
    }

    public static AdminOrderItemResponse ToAdminDto(this OrderItem item)
    {
        return new AdminOrderItemResponse
        {
            Id = item.Id,
            DishName = item.DishName,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            SpecialInstructions = item.SpecialInstructions
        };
    }

    public static AdminOrderPaymentResponse ToAdminDto(this Payment payment)
    {
        return new AdminOrderPaymentResponse
        {
            Id = payment.Id,
            Provider = payment.Provider,
            Amount = payment.Amount,
            Currency = payment.Currency,
            Status = payment.Status.ToString(),
            CreatedAt = payment.CreatedAt
        };
    }
}
