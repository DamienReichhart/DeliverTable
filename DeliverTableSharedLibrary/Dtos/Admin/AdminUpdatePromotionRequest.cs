using System.ComponentModel.DataAnnotations;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableSharedLibrary.Dtos.Admin;

public class AdminUpdatePromotionRequest
{
    [Required(ErrorMessage = "Le nom est obligatoire")]
    [MaxLength(100)]
    public string Name { get; set; } = "";

    [MaxLength(500)]
    public string? Description { get; set; }

    public PromotionType PromotionType { get; set; }

    public DiscountType DiscountType { get; set; }

    [Range(0, 99999.99, ErrorMessage = "La valeur de réduction doit être comprise entre 0 et 99999,99")]
    public decimal DiscountValue { get; set; }

    public decimal? MinOrderAmount { get; set; }

    [Required(ErrorMessage = "La date de début est obligatoire")]
    public DateTime StartsAt { get; set; }

    [Required(ErrorMessage = "La date de fin est obligatoire")]
    public DateTime EndsAt { get; set; }

    public bool IsActive { get; set; } = true;
}
