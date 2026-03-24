using System.ComponentModel.DataAnnotations;

namespace DeliverTableSharedLibrary.Dtos.Order;

public class CreateOrderRequest
{
    [Required]
    public int RestaurantId { get; set; }

    [Required]
    public string OrderType { get; set; } = "Delivery";

    [Range(1, 50)]
    public int GuestCount { get; set; } = 1;

    [MaxLength(500)]
    public string DeliveryAddress { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Notes { get; set; } = string.Empty;

    public List<string> DiscountCodes { get; set; } = [];

    [Range(0, int.MaxValue)]
    public int LoyaltyPointsToRedeem { get; set; }
}
