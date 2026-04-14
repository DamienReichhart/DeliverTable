namespace DeliverTableSharedLibrary.Dtos.Payment;

public record CreateOrderResponse(
    int OrderId,
    string ClientSecret,
    string PublishableKey,
    decimal Amount,
    string Currency);
