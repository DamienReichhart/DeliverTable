using System.ComponentModel.DataAnnotations;

namespace DeliverTableSharedLibrary.Dtos.Restaurant;

public class UpdateRestaurantOpeningHoursRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "La duree du creneau doit etre au moins 1 minute")]
    public int SlotDurationMinutes { get; set; } = 60;

    public List<OpeningDayScheduleDto> Days { get; set; } = [];
}
