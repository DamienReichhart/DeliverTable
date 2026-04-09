namespace DeliverTableInfrastructure.TemplateData;

public record OrderStatusUpdateData(
    int OrderId,
    string NewStatus,
    string RestaurantName);
