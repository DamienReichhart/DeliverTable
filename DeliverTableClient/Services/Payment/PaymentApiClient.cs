using System.Net.Http.Json;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Dtos.Order;
using DeliverTableSharedLibrary.Dtos.Payment;

namespace DeliverTableClient.Services.Payment;

public class PaymentApiClient(HttpClient http) : IPaymentApiClient
{
    public async Task<CreateOrderResponse?> CreateOrderAsync(CreateOrderRequest request)
    {
        var response = await http.PostAsJsonAsync(ApiRoutes.Order.Base, request);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<CreateOrderResponse>();
    }

    public async Task CancelAsync(int orderId)
    {
        await http.PostAsync($"{ApiRoutes.Payment.Base}/{orderId}/cancel", null);
    }
}
