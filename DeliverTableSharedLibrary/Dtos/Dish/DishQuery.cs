namespace DeliverTableSharedLibrary.Dtos.Dish;

public class DishQuery
{
    public string? Name { get; set; } = null;
    public int? LessThanPrice { get; set; } = null;
    public bool? IsVegetarian { get; set; } = null;
    public bool? IsVegan { get; set; } = null;
    public bool? IsGlutenFree { get; set; } = null;
    public bool? IsAllergenHazard { get; set; } = null;
    public bool? IsDishOfTheDay { get; set; } = null;
    public int PageSize { get; set; } = 100;
    public int PageNumber { get; set; } = 1;
}
