namespace DeliverTableSharedLibrary.Dtos.Admin;

public class AdminOrderRuleResponse
{
    public int Id { get; set; }
    public int RestaurantId { get; set; }
    public string RestaurantName { get; set; } = "";
    public decimal? MinConfirmAmount { get; set; }
    public int? MinLeadTimeHours { get; set; }
    public int? MaxAdvanceDays { get; set; }
    public int? SlotDurationMinutes { get; set; }
    public string AvailabilityRanges { get; set; } = "";
    public bool AllowPreorder { get; set; }
    public bool AllowDelivery { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
