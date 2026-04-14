using System.ComponentModel.DataAnnotations;

namespace DeliverTableSharedLibrary.Dtos.Admin;

public class AdminCreateBlockedSlotRequest
{
    [Required(ErrorMessage = "Le restaurant est obligatoire")]
    public int RestaurantId { get; set; }

    public int? RestaurantTableId { get; set; }

    [Required(ErrorMessage = "La date de début est obligatoire")]
    public DateTime StartsAt { get; set; }

    [Required(ErrorMessage = "La date de fin est obligatoire")]
    public DateTime EndsAt { get; set; }

    [MaxLength(500)]
    public string Reason { get; set; } = "";
}
