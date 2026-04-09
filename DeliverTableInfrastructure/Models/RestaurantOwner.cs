namespace DeliverTableInfrastructure.Models;

public class RestaurantOwner
{
    public int Id { get; set; } // User.Id
    public User User { get; init; } = null!;
    public string CompanyName { get; set; } = "";
    public string VatNumber { get; set; } = "";
    public string ContactPhoneNumber { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}