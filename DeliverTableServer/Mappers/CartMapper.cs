using DeliverTableServer.Models;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Dtos.Cart;

namespace DeliverTableServer.Mappers;

public static class CartMapper
{

    public static CartDto ToDto(this Cart cart)
    {
        var items = cart.Items.Select(i => i.ToDto()).ToList();
        return new CartDto
        {
            Id = cart.Id,
            RestaurantId = cart.RestaurantId,
            RestaurantName = cart.Restaurant?.Name ?? string.Empty,
            Items = items,
            TotalAmount = items.Sum(i => i.Subtotal),
            TotalItems = items.Sum(i => i.Quantity)
        };
    }

    public static CartItemDto ToDto(this CartItem item)
    {
        return new CartItemDto
        {
            Id = item.Id,
            DishId = item.DishId,
            DishName = item.Dish?.Name ?? string.Empty,
            DishImage = ApiRoutes.Dish.ImageRoute + item.DishId + UploadLimits.DefaultImageExtension,
            UnitPrice = item.UnitPrice,
            Quantity = item.Quantity,
            SpecialInstructions = item.SpecialInstructions
        };
    }
}
