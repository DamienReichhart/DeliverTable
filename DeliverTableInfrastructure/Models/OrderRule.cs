using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeliverTableInfrastructure.Models;

public class OrderRule
{
    [Key]
    public int Id { get; set; }

    public int RestaurantId { get; set; }

    [ForeignKey("RestaurantId")]
    public Restaurant Restaurant { get; set; } = null!;

    [Column(TypeName = "decimal(9, 2)")]
    public decimal? MinConfirmAmount { get; set; }

    public int? MinLeadTimeHours { get; set; }

    public int? MaxAdvanceDays { get; set; }

    public int? SlotDurationMinutes { get; set; }

    [MaxLength(2000)]
    public string AvailabilityRanges { get; set; } = string.Empty;

    public bool AllowPreorder { get; set; }

    public bool AllowDelivery { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
