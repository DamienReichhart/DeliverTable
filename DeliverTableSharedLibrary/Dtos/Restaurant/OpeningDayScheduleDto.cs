namespace DeliverTableSharedLibrary.Dtos.Restaurant;

public class OpeningDayScheduleDto
{
    public int DayOfWeek { get; set; }
    public List<OpeningHourSlotDto> Slots { get; set; } = [];
}
