namespace DeliverTableSharedLibrary.Dtos.Order;

public class OrderItemDto
{
    public int Id { get; set; }
    public int DishId { get; set; }
    public string DishName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public string SpecialInstructions { get; set; } = string.Empty;
    public decimal Subtotal => UnitPrice * Quantity;
}
