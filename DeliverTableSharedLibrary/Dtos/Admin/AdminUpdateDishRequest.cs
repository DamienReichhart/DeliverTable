using System.ComponentModel.DataAnnotations;

namespace DeliverTableSharedLibrary.Dtos.Admin;

public class AdminUpdateDishRequest
{
    [Required(ErrorMessage = "Le nom est obligatoire")]
    [MaxLength(200)]
    public string Name { get; set; } = "";

    public string? Description { get; set; }

    [Required(ErrorMessage = "Le prix est obligatoire")]
    [Range(0, 99999.99, ErrorMessage = "Le prix doit être compris entre 0 et 99999,99")]
    public decimal BasePrice { get; set; }

    public bool IsVegetarian { get; set; }
    public bool IsVegan { get; set; }
    public bool IsGlutenFree { get; set; }
    public bool IsAllergenHazard { get; set; }
    public bool IsDishOfTheDay { get; set; }
    public bool IsActive { get; set; } = true;
}
