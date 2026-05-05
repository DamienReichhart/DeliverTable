namespace DeliverTableSharedLibrary.Dtos.Restaurant;

public class RestaurantAvailableSlotsResponse
{
    public int RestaurantId { get; set; }
    public DateTime Date { get; set; }
    public int SlotDurationMinutes { get; set; }
    public List<AvailableSlotDto> Slots { get; set; } = [];
}
