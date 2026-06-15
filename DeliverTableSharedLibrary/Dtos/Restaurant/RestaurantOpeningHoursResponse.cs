namespace DeliverTableSharedLibrary.Dtos.Restaurant;

public class RestaurantOpeningHoursResponse
{
    public int RestaurantId { get; set; }
    public int SlotDurationMinutes { get; set; }
    public List<OpeningDayScheduleDto> Days { get; set; } = [];
}
