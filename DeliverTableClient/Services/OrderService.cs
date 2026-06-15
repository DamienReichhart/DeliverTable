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
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync(ApiRoutes.Order.Base, request, ct);
        if (!response.IsSuccessStatusCode)
        {
            ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: ct);
            return (null, error);
        }

        OrderDto? result = await response.Content.ReadFromJsonAsync<OrderDto>(cancellationToken: ct);
        return (result, null);
    }

    public async Task<(OrderDto?, ErrorResponse?)> GetOrderByIdAsync(int orderId, CancellationToken ct = default)
    {
        try
        {
            OrderDto? result = await _httpClient.GetFromJsonAsync<OrderDto>(
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
            List<string> queryParams = new List<string>
            {
                $"PageNumber={query.PageNumber}",
                $"PageSize={query.PageSize}"
            };
            if (!string.IsNullOrWhiteSpace(query.Status))
                queryParams.Add($"Status={Uri.EscapeDataString(query.Status)}");

            string url = $"{ApiRoutes.Order.Base}?{string.Join("&", queryParams)}";
            PaginatedResult<OrderDto>? result = await _httpClient.GetFromJsonAsync<PaginatedResult<OrderDto>>(url, ct);
            return (result, null);
        }
        catch (Exception ex)
        {
            return (null, new ErrorResponse { Error = ex.Message });
        }
    }

    public async Task<(OrderDto?, ErrorResponse?)> CancelOrderAsync(int orderId, CancellationToken ct = default)
    {
        HttpResponseMessage response = await _httpClient.DeleteAsync($"{ApiRoutes.Order.Base}/{orderId}", ct);
        if (!response.IsSuccessStatusCode)
        {
            ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: ct);
            return (null, error);
        }

        OrderDto? result = await response.Content.ReadFromJsonAsync<OrderDto>(cancellationToken: ct);
        return (result, null);
    }

    public async Task<(PaginatedResult<OrderDto>?, ErrorResponse?)> GetRestaurantOrdersAsync(int restaurantId, OrderQuery query, CancellationToken ct = default)
    {
        try
        {
            List<string> queryParams = new List<string>
            {
                $"PageNumber={query.PageNumber}",
                $"PageSize={query.PageSize}"
            };
            if (!string.IsNullOrWhiteSpace(query.Status))
                queryParams.Add($"Status={Uri.EscapeDataString(query.Status)}");

            if (query.ToPrepare.HasValue)
                queryParams.Add($"ToPrepare={query.ToPrepare.Value}");

            if (query.CreatedAfter.HasValue)
                queryParams.Add($"CreatedAfter={query.CreatedAfter.Value:O}");

            string url = $"{ApiRoutes.Order.Base}/restaurant/{restaurantId}?{string.Join("&", queryParams)}";
            PaginatedResult<OrderDto>? result = await _httpClient.GetFromJsonAsync<PaginatedResult<OrderDto>>(url, ct);
            return (result, null);
        }
        catch (Exception ex)
        {
            return (null, new ErrorResponse { Error = ex.Message });
        }
    }

    public async Task<(OrderDto?, ErrorResponse?)> UpdateOrderStatusAsync(int orderId, string status, CancellationToken ct = default)
    {
        UpdateOrderStatusRequest request = new UpdateOrderStatusRequest { Status = status };
        HttpResponseMessage response = await _httpClient.PutAsJsonAsync($"{ApiRoutes.Order.Base}/{orderId}/status", request, ct);
        if (!response.IsSuccessStatusCode)
        {
            ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: ct);
            return (null, error);
        }

        OrderDto? result = await response.Content.ReadFromJsonAsync<OrderDto>(cancellationToken: ct);
        return (result, null);
    }
}
