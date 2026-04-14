namespace DeliverTableSharedLibrary.Dtos.Admin;

public class AdminOrderResponse
{
    public int Id { get; set; }
    public string OrderType { get; set; } = "";
    public string Status { get; set; } = "";
    public string PaymentStatus { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public int LoyaltyPointsUsed { get; set; }
    public int LoyaltyPointsEarned { get; set; }
    public int GuestCount { get; set; }
    public string DeliveryAddress { get; set; } = "";
    public string Notes { get; set; } = "";
    public DateTime? ScheduledAt { get; set; }
    public bool IsEventBooking { get; set; }
    public string Source { get; set; } = "";
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = "";
    public int RestaurantId { get; set; }
    public string RestaurantName { get; set; } = "";
    public List<AdminOrderItemResponse> Items { get; set; } = [];
    public List<AdminOrderPaymentResponse> Payments { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
