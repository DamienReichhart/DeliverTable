using System.ComponentModel.DataAnnotations;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableSharedLibrary.Dtos.Admin;

public class AdminUpdateDiscountCodeRequest
{
    [MaxLength(500)]
    public string? Description { get; set; }

    public DiscountType DiscountType { get; set; }

    [Range(0, 99999.99, ErrorMessage = "La valeur de réduction doit être comprise entre 0 et 99999,99")]
    public decimal DiscountValue { get; set; }

    public decimal? MinOrderAmount { get; set; }

    [Required(ErrorMessage = "La date de début est obligatoire")]
    public DateTime ValidFrom { get; set; }

    [Required(ErrorMessage = "La date de fin est obligatoire")]
    public DateTime ValidUntil { get; set; }

    public int? MaxRedemptions { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "La limite par utilisateur doit être au moins 1")]
    public int PerUserLimit { get; set; } = 1;

    public bool IsActive { get; set; } = true;
}
