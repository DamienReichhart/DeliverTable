using DeliverTableInfrastructure.Models;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableServer.Mappers;

public static class AdminDishMapper
{
    public static AdminDishResponse ToAdminDto(this Dish dish)
    {
        return new AdminDishResponse
        {
            Id = dish.Id,
            Name = dish.Name,
            Description = dish.Description ?? "",
            BasePrice = dish.BasePrice,
            IsVegetarian = dish.IsVegetarian,
            IsVegan = dish.IsVegan,
            IsGlutenFree = dish.IsGlutenFree,
            IsAllergenHazard = dish.IsAllergenHazard,
            IsDishOfTheDay = dish.IsDishOfTheDay,
            IsActive = dish.IsActive,
            RestaurantId = dish.RestaurantId,
            RestaurantName = dish.Restaurant is not null
                ? dish.Restaurant.Name
                : "",
            CreatedAt = dish.CreatedAt,
            UpdatedAt = dish.UpdatedAt
        };
    }
}
