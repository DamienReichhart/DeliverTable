using DeliverTableSharedLibrary.Enums;

namespace DeliverTableSharedLibrary.Dtos.Dish;

public class DishDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal BasePrice { get; set; } = 0;
    public bool IsVegetarian { get; set; } = false;
    public bool IsVegan { get; set; } = false;
    public bool IsGlutenFree { get; set; } = false;
    public bool IsAllergenHazard { get; set; } = false;
    public bool IsDishOfTheDay { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public string Image { get; set; } = string.Empty;
    public VatRate VatRate { get; set; } = VatRate.Intermediate10;
}
