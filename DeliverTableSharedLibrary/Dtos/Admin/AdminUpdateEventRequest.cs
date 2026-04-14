using System.ComponentModel.DataAnnotations;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableSharedLibrary.Dtos.Admin;

public class AdminUpdateEventRequest
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

    public EventVisibility Visibility { get; set; } = EventVisibility.Public;

    public bool IsActive { get; set; } = true;

    public int? RestaurantId { get; set; }
}
