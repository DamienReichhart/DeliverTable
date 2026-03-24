using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeliverTableServer.Models;

public class PromotionDish
{
    [Key]
    public int Id { get; set; }

    public int PromotionId { get; set; }

    [ForeignKey("PromotionId")]
    public Promotion Promotion { get; set; } = null!;

    public int DishId { get; set; }

    [ForeignKey("DishId")]
    public Dish Dish { get; set; } = null!;
}
