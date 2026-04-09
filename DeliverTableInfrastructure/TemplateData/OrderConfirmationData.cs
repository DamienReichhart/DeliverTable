namespace DeliverTableInfrastructure.TemplateData;

public record OrderConfirmationData(
    int OrderId,
    decimal TotalPrice,
    string RestaurantName,
    List<OrderItemData> Items);

public record OrderItemData(
    string DishName,
    int Quantity,
    decimal UnitPrice);
