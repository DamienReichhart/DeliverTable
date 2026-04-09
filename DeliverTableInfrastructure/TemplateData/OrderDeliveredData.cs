namespace DeliverTableInfrastructure.TemplateData;

public record OrderDeliveredData(
    int OrderId,
    decimal TotalPrice,
    string RestaurantName);
