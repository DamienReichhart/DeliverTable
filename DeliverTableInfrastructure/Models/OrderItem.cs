using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeliverTableInfrastructure.Models;

public class OrderItem
{
    [Key]
    public int Id { get; set; }

    public int OrderId { get; set; }

    [ForeignKey("OrderId")]
    public Order Order { get; set; } = null!;

    public int DishId { get; set; }

    [ForeignKey("DishId")]
    public Dish Dish { get; set; } = null!;

    [Required]
    [MaxLength(255)]
    public string DishName { get; set; } = string.Empty;

    [Range(1, 99)]
    public int Quantity { get; set; } = 1;

    [Range(0, 99999.99)]
    public decimal UnitPrice { get; set; }

    [MaxLength(500)]
    public string SpecialInstructions { get; set; } = string.Empty;
}
