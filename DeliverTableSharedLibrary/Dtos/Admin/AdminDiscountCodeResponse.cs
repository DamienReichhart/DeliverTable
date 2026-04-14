using DeliverTableSharedLibrary.Enums;

namespace DeliverTableSharedLibrary.Dtos.Admin;

public class AdminDiscountCodeResponse
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Description { get; set; } = "";
    public DiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidUntil { get; set; }
    public int? MaxRedemptions { get; set; }
    public int PerUserLimit { get; set; }
    public int CurrentRedemptions { get; set; }
    public bool IsActive { get; set; }
    public int RestaurantId { get; set; }
    public string RestaurantName { get; set; } = "";
    public int RedemptionCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
