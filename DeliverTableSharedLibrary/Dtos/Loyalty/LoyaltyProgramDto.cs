namespace DeliverTableSharedLibrary.Dtos.Loyalty;

public class LoyaltyProgramDto
{
    public int Id { get; set; }
    public int RestaurantId { get; set; }
    public decimal PointsPerEuro { get; set; }
    public decimal EurosPerPoint { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
