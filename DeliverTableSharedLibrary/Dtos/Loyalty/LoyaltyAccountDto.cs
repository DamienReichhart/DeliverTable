namespace DeliverTableSharedLibrary.Dtos.Loyalty;

public class LoyaltyAccountDto
{
    public int Id { get; set; }
    public int PointsBalance { get; set; }
    public decimal EuroEquivalent { get; set; }
    public decimal PointsPerEuro { get; set; }
    public decimal EurosPerPoint { get; set; }
}
