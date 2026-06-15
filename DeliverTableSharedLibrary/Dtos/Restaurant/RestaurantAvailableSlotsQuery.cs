using System.ComponentModel.DataAnnotations;

namespace DeliverTableSharedLibrary.Dtos.Restaurant;

public class RestaurantAvailableSlotsQuery
{
    [Required]
    public DateTime Date { get; set; }

    [Range(1, 50)]
    public int GuestCount { get; set; } = 2;
}
