using System.ComponentModel.DataAnnotations;

namespace DeliverTableSharedLibrary.Dtos.Admin;

public class AdminCreateLoyaltyProgramRequest
{
    [Required(ErrorMessage = "Le nombre de points par euro est obligatoire")]
    [Range(0.01, 99999.99, ErrorMessage = "Le nombre de points par euro doit être compris entre 0,01 et 99999,99")]
    public decimal PointsPerEuro { get; set; }

    [Required(ErrorMessage = "La valeur en euros par point est obligatoire")]
    [Range(0.0001, 99999.9999, ErrorMessage = "La valeur en euros par point doit être comprise entre 0,0001 et 99999,9999")]
    public decimal EurosPerPoint { get; set; }

    [Required(ErrorMessage = "Le restaurant est obligatoire")]
    public int RestaurantId { get; set; }

    public bool IsActive { get; set; } = true;
}
