using System.ComponentModel.DataAnnotations;

namespace DeliverTableSharedLibrary.Dtos.Event;

public class UpdateRestaurantEventRequest
{
    [Required(ErrorMessage = "Le nom est obligatoire")]
    [MaxLength(200)]
    public string Name { get; set; } = "";

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "La date de début est obligatoire")]
    public DateTime StartsAt { get; set; }

    [Required(ErrorMessage = "La date de fin est obligatoire")]
    public DateTime EndsAt { get; set; }

    public int? MaxGuests { get; set; }

    public bool IsActive { get; set; } = true;

    public List<EventMenuItemRequest> MenuItems { get; set; } = [];
}
