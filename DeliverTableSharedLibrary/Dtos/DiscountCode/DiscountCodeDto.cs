namespace DeliverTableSharedLibrary.Dtos.DiscountCode;

public class DiscountCodeDto
{
    public int Id { get; set; }
    public int RestaurantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DiscountType { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidUntil { get; set; }
    public int? MaxRedemptions { get; set; }
    public int PerUserLimit { get; set; }
    public int CurrentRedemptions { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
