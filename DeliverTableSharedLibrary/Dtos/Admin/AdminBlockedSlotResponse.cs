namespace DeliverTableSharedLibrary.Dtos.Admin;

public class AdminBlockedSlotResponse
{
    public int Id { get; set; }
    public int RestaurantId { get; set; }
    public string RestaurantName { get; set; } = "";
    public int? RestaurantTableId { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public string Reason { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
