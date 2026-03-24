using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableServer.Models;

public class DiscountCode
{
    [Key]
    public int Id { get; set; }

    public int RestaurantId { get; set; }

    [ForeignKey("RestaurantId")]
    public Restaurant Restaurant { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public DiscountType DiscountType { get; set; }

    [Column(TypeName = "decimal(9, 2)")]
    public decimal DiscountValue { get; set; }

    [Column(TypeName = "decimal(9, 2)")]
    public decimal? MinOrderAmount { get; set; }

    public DateTime ValidFrom { get; set; }

    public DateTime ValidUntil { get; set; }

    public int? MaxRedemptions { get; set; }

    public int PerUserLimit { get; set; } = 1;

    public int CurrentRedemptions { get; set; }

    public bool IsActive { get; set; } = true;

    public List<DiscountCodeRedemption> Redemptions { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
