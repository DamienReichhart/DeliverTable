using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeliverTableInfrastructure.Models;

public class Refund
{
    [Key]
    public int Id { get; set; }

    public int PaymentId { get; set; }

    [ForeignKey(nameof(PaymentId))]
    public Payment Payment { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string StripeRefundId { get; set; } = string.Empty;

    [Column(TypeName = "decimal(9, 2)")]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "EUR";

    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;

    public int? CreatedByUserId { get; set; }

    [ForeignKey(nameof(CreatedByUserId))]
    public User? CreatedByUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
