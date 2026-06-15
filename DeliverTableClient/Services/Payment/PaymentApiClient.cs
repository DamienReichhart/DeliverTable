using System.Net.Http.Json;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Order;
using DeliverTableSharedLibrary.Dtos.Payment;

namespace DeliverTableClient.Services.Payment;

public class PaymentApiClient(HttpClient http) : IPaymentApiClient
{
    public async Task<(CreateOrderResponse?, ErrorResponse?)> CreateOrderAsync(CreateOrderRequest request)
    {
        HttpResponseMessage response = await http.PostAsJsonAsync(ApiRoutes.Order.Base, request);
        if (!response.IsSuccessStatusCode)
        {
            ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            return (null, error);
        }
        CreateOrderResponse? result = await response.Content.ReadFromJsonAsync<CreateOrderResponse>();
        return (result, null);
    }

    public async Task CancelAsync(int orderId)
    {
        await http.PostAsync($"{ApiRoutes.Payment.Base}/{orderId}/cancel", null);
    }
}
