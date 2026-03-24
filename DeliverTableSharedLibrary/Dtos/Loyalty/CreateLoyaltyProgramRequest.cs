using System.ComponentModel.DataAnnotations;

namespace DeliverTableSharedLibrary.Dtos.Loyalty;

public class CreateLoyaltyProgramRequest
{
    [Required] [Range(0.01, 999.99)]
    public decimal PointsPerEuro { get; set; } = 1.0m;
    [Required] [Range(0.0001, 999.99)]
    public decimal EurosPerPoint { get; set; } = 0.10m;
}
