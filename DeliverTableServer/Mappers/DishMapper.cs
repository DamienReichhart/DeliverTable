using DeliverTableServer.Models;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Dtos.Dish;

namespace DeliverTableServer.Mappers;

public static class DishMapper
{
    private const string _defaultFileExtension = ".png";

    public static DishDto ToDto(this Dish dish)
    {
        return new DishDto
        {
            Id = dish.Id,
            Name = dish.Name,
            Description = dish.Description,
            BasePrice = dish.BasePrice,
            IsVegetarian = dish.IsVegetarian,
            IsVegan = dish.IsVegan,
            IsGlutenFree = dish.IsGlutenFree,
            IsAllergenHazard = dish.IsAllergenHazard,
            IsDishOfTheDay = dish.IsDishOfTheDay,
            IsActive = dish.IsActive,
            Image = ApiRoutes.Dish.ImageRoute + dish.Id + _defaultFileExtension
        };
    }
}