using System.ComponentModel.DataAnnotations;

namespace DeliverTableSharedLibrary.Dtos.Order;

public class CreateOrderRequest
{
    [Required]
    public int RestaurantId { get; set; }

    [Required]
    public string OrderType { get; set; } = "Delivery";

    [Range(1, 500)]
    public int GuestCount { get; set; } = 1;

    [MaxLength(500)]
    public string DeliveryAddress { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Notes { get; set; } = string.Empty;

    public DateTime? ScheduledAt { get; set; }

    public bool IsEventBooking { get; set; }

    public int? EventId { get; set; }

    [MaxLength(200)]
    public string EventName { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string EventDescription { get; set; } = string.Empty;

    public DateTime? EventStartsAt { get; set; }

    public DateTime? EventEndsAt { get; set; }

    public List<string> DiscountCodes { get; set; } = [];

    [Range(0, int.MaxValue)]
    public int LoyaltyPointsToRedeem { get; set; }
}
