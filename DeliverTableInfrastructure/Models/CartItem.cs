using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeliverTableInfrastructure.Models;

public class CartItem
{
    [Key]
    public int Id { get; set; }

    public int CartId { get; set; }

    [ForeignKey("CartId")]
    public Cart Cart { get; set; } = null!;

    public int DishId { get; set; }

    [ForeignKey("DishId")]
    public Dish Dish { get; set; } = null!;

    [Range(1, 99)]
    public int Quantity { get; set; } = 1;

    [Range(0, 99999.99)]
    public decimal UnitPrice { get; set; }

    [MaxLength(500)]
    public string SpecialInstructions { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
