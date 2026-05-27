using System.ComponentModel.DataAnnotations;

namespace DeliverTableSharedLibrary.Dtos.Promotion;

public class UpdatePromotionRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
    [Required]
    public string PromotionType { get; set; } = string.Empty;
    [Required]
    public string DiscountType { get; set; } = string.Empty;
    [Required]
    [Range(0, 999999.99)]
    public decimal DiscountValue { get; set; }
    [Range(0, 999999.99)]
    public decimal? MinOrderAmount { get; set; }
    [Required]
    public DateTime StartsAt { get; set; }
    [Required]
    public DateTime EndsAt { get; set; }
    public bool IsActive { get; set; } = true;
    public List<int> DishIds { get; set; } = [];
}
