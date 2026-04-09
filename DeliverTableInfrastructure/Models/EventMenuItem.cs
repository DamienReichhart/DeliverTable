using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeliverTableInfrastructure.Models;

public class EventMenuItem
{
    [Key]
    public int Id { get; set; }

    public int EventId { get; set; }

    [ForeignKey("EventId")]
    public Event Event { get; set; } = null!;

    public int DishId { get; set; }

    [ForeignKey("DishId")]
    public Dish Dish { get; set; } = null!;

    [Column(TypeName = "decimal(7, 2)")]
    public decimal? OverridePrice { get; set; }
}
