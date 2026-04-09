namespace DeliverTableInfrastructure.TemplateData;

public record NewOrderForRestaurantData(
    int OrderId,
    string CustomerName,
    string OrderType,
    decimal TotalAmount,
    List<OrderItemData> Items);
