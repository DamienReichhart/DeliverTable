using System.Net.Http.Json;
using DeliverTableClient.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Cart;

namespace DeliverTableClient.Services;

public sealed class CartService(HttpClient httpClient) : ICartService
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<(CartDto?, ErrorResponse?)> GetCartAsync(int restaurantId, CancellationToken ct = default)
    {
        try
        {
            CartDto? result = await _httpClient.GetFromJsonAsync<CartDto>(
                $"{ApiRoutes.Cart.Base}/restaurant/{restaurantId}", ct);
            return (result, null);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return (new CartDto { RestaurantId = restaurantId }, null);
        }
        catch (Exception ex)
        {
            return (null, new ErrorResponse { Error = ex.Message });
        }
    }

    public async Task<(List<CartDto>?, ErrorResponse?)> GetAllCartsAsync(CancellationToken ct = default)
    {
        try
        {
            List<CartDto>? result = await _httpClient.GetFromJsonAsync<List<CartDto>>(ApiRoutes.Cart.Base, ct);
            return (result, null);
        }
        catch (Exception ex)
        {
            return (null, new ErrorResponse { Error = ex.Message });
        }
    }

    public async Task<(CartDto?, ErrorResponse?)> AddItemAsync(AddToCartRequest request, CancellationToken ct = default)
    {
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync(ApiRoutes.Cart.Items, request, ct);
        if (!response.IsSuccessStatusCode)
        {
            ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: ct);
            return (null, error);
        }

        CartDto? result = await response.Content.ReadFromJsonAsync<CartDto>(cancellationToken: ct);
        return (result, null);
    }

    public async Task<(CartItemDto?, ErrorResponse?)> UpdateItemAsync(int cartItemId, UpdateCartItemRequest request, CancellationToken ct = default)
    {
        HttpResponseMessage response = await _httpClient.PutAsJsonAsync($"{ApiRoutes.Cart.Base}/items/{cartItemId}", request, ct);
        if (!response.IsSuccessStatusCode)
        {
            ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: ct);
            return (null, error);
        }

        CartItemDto? result = await response.Content.ReadFromJsonAsync<CartItemDto>(cancellationToken: ct);
        return (result, null);
    }

    public async Task<ErrorResponse?> RemoveItemAsync(int cartItemId, CancellationToken ct = default)
    {
        HttpResponseMessage response = await _httpClient.DeleteAsync($"{ApiRoutes.Cart.Base}/items/{cartItemId}", ct);
        if (!response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: ct);
        }
        return null;
    }

    public async Task<ErrorResponse?> ClearCartAsync(int restaurantId, CancellationToken ct = default)
    {
        HttpResponseMessage response = await _httpClient.DeleteAsync($"{ApiRoutes.Cart.Base}/restaurant/{restaurantId}", ct);
        if (!response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: ct);
        }
        return null;
    }
}
