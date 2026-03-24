namespace DeliverTableSharedLibrary.Dtos.Admin;

public class AdminLoyaltyProgramResponse
{
    public int Id { get; set; }
    public decimal PointsPerEuro { get; set; }
    public decimal EurosPerPoint { get; set; }
    public bool IsActive { get; set; }
    public int RestaurantId { get; set; }
    public string RestaurantName { get; set; } = "";
    public int AccountCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
