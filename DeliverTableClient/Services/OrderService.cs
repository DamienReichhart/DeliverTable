using System.Net.Http.Json;
using DeliverTableClient.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Order;

namespace DeliverTableClient.Services;

public sealed class OrderService(HttpClient httpClient) : IOrderService
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<(OrderDto?, ErrorResponse?)> CreateOrderAsync(CreateOrderRequest request, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync(ApiRoutes.Order.Base, request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: ct);
            return (null, error);
        }

        var result = await response.Content.ReadFromJsonAsync<OrderDto>(cancellationToken: ct);
        return (result, null);
    }

    public async Task<(OrderDto?, ErrorResponse?)> GetOrderByIdAsync(int orderId, CancellationToken ct = default)
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<OrderDto>(
                $"{ApiRoutes.Order.Base}/{orderId}", ct);
            return (result, null);
        }
        catch (Exception ex)
        {
            return (null, new ErrorResponse { Error = ex.Message });
        }
    }

    public async Task<(PaginatedResult<OrderDto>?, ErrorResponse?)> GetMyOrdersAsync(OrderQuery query, CancellationToken ct = default)
    {
        try
        {
            var queryParams = new List<string>
            {
                $"PageNumber={query.PageNumber}",
                $"PageSize={query.PageSize}"
            };
            if (!string.IsNullOrWhiteSpace(query.Status))
                queryParams.Add($"Status={Uri.EscapeDataString(query.Status)}");

            var url = $"{ApiRoutes.Order.Base}?{string.Join("&", queryParams)}";
            var result = await _httpClient.GetFromJsonAsync<PaginatedResult<OrderDto>>(url, ct);
            return (result, null);
        }
        catch (Exception ex)
        {
            return (null, new ErrorResponse { Error = ex.Message });
        }
    }

    public async Task<(OrderDto?, ErrorResponse?)> CancelOrderAsync(int orderId, CancellationToken ct = default)
    {
        var response = await _httpClient.DeleteAsync($"{ApiRoutes.Order.Base}/{orderId}", ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: ct);
            return (null, error);
        }

        var result = await response.Content.ReadFromJsonAsync<OrderDto>(cancellationToken: ct);
        return (result, null);
    }
}
