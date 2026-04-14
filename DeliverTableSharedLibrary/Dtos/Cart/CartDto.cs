namespace DeliverTableSharedLibrary.Dtos.Cart;

public class CartDto
{
    public int Id { get; set; }
    public int RestaurantId { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public List<CartItemDto> Items { get; set; } = [];
    public decimal TotalAmount { get; set; }
    public int TotalItems { get; set; }
}
