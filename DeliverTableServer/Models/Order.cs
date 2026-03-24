using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableServer.Models;

public class Order
{
    [Key]
    public int Id { get; set; }

    public int CustomerId { get; set; }

    [ForeignKey("CustomerId")]
    public User Customer { get; set; } = null!;

    public int RestaurantId { get; set; }

    [ForeignKey("RestaurantId")]
    public Restaurant Restaurant { get; set; } = null!;

    public OrderType OrderType { get; set; } = OrderType.Delivery;

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    [Range(0, 999999.99)]
    public decimal TotalAmount { get; set; }

    [Range(0, 999999.99)]
    public decimal OriginalAmount { get; set; }

    [Range(0, 999999.99)]
    public decimal DiscountAmount { get; set; }

    public int LoyaltyPointsUsed { get; set; }

    public int LoyaltyPointsEarned { get; set; }

    public int? DiscountCodeId { get; set; }

    [ForeignKey("DiscountCodeId")]
    public DiscountCode? DiscountCode { get; set; }

    public List<OrderDiscount> Discounts { get; set; } = [];

    [Range(1, 50)]
    public int GuestCount { get; set; } = 1;

    [MaxLength(500)]
    public string DeliveryAddress { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Notes { get; set; } = string.Empty;

    public BookingSource Source { get; set; } = BookingSource.CustomerApp;

    public List<OrderItem> Items { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
