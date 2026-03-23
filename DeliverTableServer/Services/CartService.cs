using DeliverTableServer.Common;
using DeliverTableServer.Constants;
using DeliverTableServer.Mappers;
using DeliverTableServer.Models;
using DeliverTableServer.Repositories.Interfaces;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Cart;

namespace DeliverTableServer.Services;

public sealed class CartService(
    ICartRepository cartRepository,
    IDishRepository dishRepository,
    IRestaurantRepository restaurantRepository
) : ICartService
{
    private readonly ICartRepository _cartRepository = cartRepository;
    private readonly IDishRepository _dishRepository = dishRepository;
    private readonly IRestaurantRepository _restaurantRepository = restaurantRepository;

    public async Task<ServiceResult<CartDto>> GetCartAsync(int customerId, int restaurantId, CancellationToken ct = default)
    {
        var cart = await _cartRepository.GetByCustomerAndRestaurantAsync(customerId, restaurantId, ct);
        if (cart is null)
        {
            return new CartDto
            {
                RestaurantId = restaurantId,
                Items = [],
                TotalAmount = 0,
                TotalItems = 0
            };
        }

        return cart.ToDto();
    }

    public async Task<ServiceResult<List<CartDto>>> GetAllCartsAsync(int customerId, CancellationToken ct = default)
    {
        var carts = await _cartRepository.GetByCustomerAsync(customerId, ct);
        return carts.Select(c => c.ToDto()).ToList();
    }

    public async Task<ServiceResult<CartDto>> AddItemAsync(int customerId, AddToCartRequest request, CancellationToken ct = default)
    {
        var restaurant = await _restaurantRepository.GetByIdAsync(request.RestaurantId, ct);
        if (restaurant is null)
            return new ServiceError(ErrorMessages.RestaurantNotFound, 404);

        if (!restaurant.IsActive)
            return new ServiceError(ErrorMessages.RestaurantNotActive);

        var dish = await _dishRepository.GetByIdAsync(request.DishId, ct);
        if (dish is null)
            return new ServiceError(ErrorMessages.DishNotFound, 404);

        if (!dish.IsActive)
            return new ServiceError(ErrorMessages.DishNotAvailable);

        if (dish.RestaurantId != request.RestaurantId)
            return new ServiceError(ErrorMessages.DishNotFromRestaurant);

        var cart = await _cartRepository.GetByCustomerAndRestaurantAsync(customerId, request.RestaurantId, ct);

        if (cart is null)
        {
            cart = new Cart
            {
                CustomerId = customerId,
                RestaurantId = request.RestaurantId
            };
            cart = await _cartRepository.CreateAsync(cart, ct);
        }

        var existingItem = cart.Items.FirstOrDefault(i => i.DishId == request.DishId);
        if (existingItem is not null)
        {
            existingItem.Quantity += request.Quantity;
            existingItem.UnitPrice = dish.BasePrice;
            if (!string.IsNullOrWhiteSpace(request.SpecialInstructions))
                existingItem.SpecialInstructions = request.SpecialInstructions;
            await _cartRepository.UpdateItemAsync(existingItem, ct);
        }
        else
        {
            var cartItem = new CartItem
            {
                CartId = cart.Id,
                DishId = request.DishId,
                Quantity = request.Quantity,
                UnitPrice = dish.BasePrice,
                SpecialInstructions = request.SpecialInstructions
            };
            await _cartRepository.AddItemAsync(cartItem, ct);
        }

        var updatedCart = await _cartRepository.GetByCustomerAndRestaurantAsync(customerId, request.RestaurantId, ct);
        return updatedCart!.ToDto();
    }

    public async Task<ServiceResult<CartItemDto>> UpdateItemAsync(int customerId, int cartItemId, UpdateCartItemRequest request, CancellationToken ct = default)
    {
        var cartItem = await _cartRepository.GetCartItemByIdAsync(cartItemId, ct);
        if (cartItem is null)
            return new ServiceError(ErrorMessages.CartItemNotFound, 404);

        if (cartItem.Cart.CustomerId != customerId)
            return new ServiceError(ErrorMessages.CartItemNotFound, 404);

        cartItem.Quantity = request.Quantity;
        cartItem.SpecialInstructions = request.SpecialInstructions;

        var updated = await _cartRepository.UpdateItemAsync(cartItem, ct);
        return updated.ToDto();
    }

    public async Task<ServiceResult> RemoveItemAsync(int customerId, int cartItemId, CancellationToken ct = default)
    {
        var cartItem = await _cartRepository.GetCartItemByIdAsync(cartItemId, ct);
        if (cartItem is null)
            return new ServiceError(ErrorMessages.CartItemNotFound, 404);

        if (cartItem.Cart.CustomerId != customerId)
            return new ServiceError(ErrorMessages.CartItemNotFound, 404);

        await _cartRepository.RemoveItemAsync(cartItemId, ct);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> ClearCartAsync(int customerId, int restaurantId, CancellationToken ct = default)
    {
        var cart = await _cartRepository.GetByCustomerAndRestaurantAsync(customerId, restaurantId, ct);
        if (cart is null)
            return new ServiceError(ErrorMessages.CartNotFound, 404);

        await _cartRepository.DeleteAsync(cart.Id, ct);
        return ServiceResult.Success();
    }
}
