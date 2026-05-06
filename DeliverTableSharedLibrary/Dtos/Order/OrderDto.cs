using DeliverTableSharedLibrary.Dtos.Payment;

namespace DeliverTableSharedLibrary.Dtos.Order;

public class OrderDto
{
    public int Id { get; set; }
    public int RestaurantId { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public string OrderType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public int LoyaltyPointsUsed { get; set; }
    public int LoyaltyPointsEarned { get; set; }
    public List<OrderDiscountDto> Discounts { get; set; } = [];
    public int GuestCount { get; set; }
    public string DeliveryAddress { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime? ScheduledAt { get; set; }
    public int? RestaurantTableId { get; set; }
    public bool IsEventBooking { get; set; }
    public int? EventId { get; set; }
    public List<OrderItemDto> Items { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? GatewayStatus { get; set; }
    public List<RefundDto> Refunds { get; set; } = [];
    public decimal TotalRefunded { get; set; }
}
