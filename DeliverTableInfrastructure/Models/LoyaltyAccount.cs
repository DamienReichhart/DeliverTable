using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeliverTableInfrastructure.Models;

public class LoyaltyAccount
{
    [Key]
    public int Id { get; set; }

    public int LoyaltyProgramId { get; set; }

    [ForeignKey("LoyaltyProgramId")]
    public LoyaltyProgram LoyaltyProgram { get; set; } = null!;

    public int CustomerId { get; set; }

    [ForeignKey("CustomerId")]
    public User Customer { get; set; } = null!;

    public int PointsBalance { get; set; }

    public List<LoyaltyTransaction> Transactions { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
