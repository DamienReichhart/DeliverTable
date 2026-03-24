using System.ComponentModel.DataAnnotations;

namespace DeliverTableSharedLibrary.Dtos.DiscountCode;

public class CreateDiscountCodeRequest
{
    [Required] [MaxLength(50)]
    public string Code { get; set; } = string.Empty;
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
    [Required]
    public string DiscountType { get; set; } = string.Empty;
    [Required] [Range(0.01, 999999.99)]
    public decimal DiscountValue { get; set; }
    [Range(0.01, 999999.99)]
    public decimal? MinOrderAmount { get; set; }
    [Required]
    public DateTime ValidFrom { get; set; }
    [Required]
    public DateTime ValidUntil { get; set; }
    [Range(1, int.MaxValue)]
    public int? MaxRedemptions { get; set; }
    [Range(1, int.MaxValue)]
    public int PerUserLimit { get; set; } = 1;
}
