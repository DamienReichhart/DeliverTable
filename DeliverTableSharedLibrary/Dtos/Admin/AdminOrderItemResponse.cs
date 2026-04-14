namespace DeliverTableSharedLibrary.Dtos.Admin;

public class AdminOrderItemResponse
{
    public int Id { get; set; }
    public string DishName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string SpecialInstructions { get; set; } = "";
}
