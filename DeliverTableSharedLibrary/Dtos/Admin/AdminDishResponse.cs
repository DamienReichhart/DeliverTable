namespace DeliverTableSharedLibrary.Dtos.Admin;

public class AdminDishResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal BasePrice { get; set; }
    public bool IsVegetarian { get; set; }
    public bool IsVegan { get; set; }
    public bool IsGlutenFree { get; set; }
    public bool IsAllergenHazard { get; set; }
    public bool IsDishOfTheDay { get; set; }
    public bool IsActive { get; set; }
    public int RestaurantId { get; set; }
    public string RestaurantName { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
