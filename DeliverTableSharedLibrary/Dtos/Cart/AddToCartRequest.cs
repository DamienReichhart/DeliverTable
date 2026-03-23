using System.ComponentModel.DataAnnotations;

namespace DeliverTableSharedLibrary.Dtos.Cart;

public class AddToCartRequest
{
    [Required]
    public int RestaurantId { get; set; }

    [Required]
    public int DishId { get; set; }

    [Range(1, 99)]
    public int Quantity { get; set; } = 1;

    [MaxLength(500)]
    public string SpecialInstructions { get; set; } = string.Empty;
}
