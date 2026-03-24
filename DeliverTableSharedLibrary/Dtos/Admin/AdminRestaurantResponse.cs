namespace DeliverTableSharedLibrary.Dtos.Admin;

public class AdminRestaurantResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string Description { get; set; } = "";
    public string AdressLine1 { get; set; } = "";
    public string AdressLine2 { get; set; } = "";
    public string City { get; set; } = "";
    public string ZipCode { get; set; } = "";
    public string Country { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool IsActive { get; set; }
    public decimal Balance { get; set; }
    public int OwnerId { get; set; }
    public string OwnerName { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
