using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableInfrastructure.Models;

public class DiscountCodeRedemption
{
    [Key]
    public int Id { get; set; }

    public int DiscountCodeId { get; set; }

    [ForeignKey("DiscountCodeId")]
    public DiscountCode DiscountCode { get; set; } = null!;

    public int CustomerId { get; set; }

    [ForeignKey("CustomerId")]
    public User Customer { get; set; } = null!;

    public int OrderId { get; set; }

    [ForeignKey("OrderId")]
    public Order Order { get; set; } = null!;

    public DiscountRedemptionStatus Status { get; set; } = DiscountRedemptionStatus.Committed;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
