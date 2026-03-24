using System.ComponentModel.DataAnnotations;

namespace DeliverTableSharedLibrary.Dtos.Dish;

public class CreateDishDto
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    [Range(0, 99999.99, ErrorMessage = "Le prix doit être entre 0 et 99999.99")]
    public decimal BasePrice { get; set; } = 0;
    public bool IsVegetarian { get; set; } = false;
    public bool IsVegan { get; set; } = false;
    public bool IsGlutenFree { get; set; } = false;
    public bool IsAllergenHazard { get; set; } = false;
    public bool IsDishOfTheDay { get; set; } = false;
    public bool IsActive { get; set; } = true;
}
