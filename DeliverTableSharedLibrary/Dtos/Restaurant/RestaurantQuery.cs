namespace DeliverTableSharedLibrary.Dtos.Restaurant;

public class RestaurantQuery
{
    public string? Name { get; set; } = null;
    public string? City { get; set; } = null;
    public string? Type { get; set; } = null;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? RadiusKm { get; set; }
    public bool IsActive { get; set; } = true;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
