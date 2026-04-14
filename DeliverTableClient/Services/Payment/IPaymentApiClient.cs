using DeliverTableSharedLibrary.Dtos.Order;
using DeliverTableSharedLibrary.Dtos.Payment;

namespace DeliverTableClient.Services.Payment;

public interface IPaymentApiClient
{
    Task<CreateOrderResponse?> CreateOrderAsync(CreateOrderRequest request);
    Task CancelAsync(int orderId);
}
