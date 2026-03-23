using System.ComponentModel.DataAnnotations;

namespace DeliverTableSharedLibrary.Dtos.Cart;

public class UpdateCartItemRequest
{
    [Range(1, 99)]
    public int Quantity { get; set; }

    [MaxLength(500)]
    public string SpecialInstructions { get; set; } = string.Empty;
}
