using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeliverTableServer.Models;

public class LoyaltyProgram
{
    [Key]
    public int Id { get; set; }

    public int RestaurantId { get; set; }

    [ForeignKey("RestaurantId")]
    public Restaurant Restaurant { get; set; } = null!;

    [Column(TypeName = "decimal(9, 2)")]
    public decimal PointsPerEuro { get; set; } = 1.0m;

    [Column(TypeName = "decimal(9, 4)")]
    public decimal EurosPerPoint { get; set; } = 0.10m;

    public bool IsActive { get; set; } = true;

    public List<LoyaltyAccount> Accounts { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
