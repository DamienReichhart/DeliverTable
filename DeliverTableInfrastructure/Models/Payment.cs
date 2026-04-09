using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableInfrastructure.Models;

public class Payment
{
    [Key]
    public int Id { get; set; }

    public int OrderId { get; set; }

    [ForeignKey("OrderId")]
    public Order Order { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string Provider { get; set; } = "Stripe";

    [MaxLength(200)]
    public string StripePaymentIntentId { get; set; } = string.Empty;

    [MaxLength(200)]
    public string StripeChargeId { get; set; } = string.Empty;

    [Column(TypeName = "decimal(9, 2)")]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "EUR";

    public PaymentGatewayStatus Status { get; set; } = PaymentGatewayStatus.RequiresPaymentMethod;

    public DateTime? AuthorizedAt { get; set; }

    public DateTime? CapturedAt { get; set; }

    public DateTime? CanceledAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
