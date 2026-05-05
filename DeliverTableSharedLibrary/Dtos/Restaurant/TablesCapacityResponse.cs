namespace DeliverTableSharedLibrary.Dtos.Restaurant;

public class TablesCapacityResponse
{
    public int RestaurantId { get; set; }
    public int CapacityPerSlot { get; set; }
    public int ActiveTablesFallback { get; set; }
}
