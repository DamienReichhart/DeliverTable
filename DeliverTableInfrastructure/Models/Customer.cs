namespace DeliverTableInfrastructure.Models;

public class Customer
{
    public int Id { get; set; } // User.Id
    public User User { get; init; } = null!;
    public string AllergyNotes { get; set; } = "";
    public string DietaryPreferences { get; set; } = "";
    public Boolean MarketingOptIn { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}