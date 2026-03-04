using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DeliverTableServer.Models;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Dtos.Dish;

namespace DeliverTableServer.Mappers
{
    public static class DishMapper
    {
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
                Image = !string.IsNullOrWhiteSpace(dish.ImageKey) ? ApiRoutes.Dish.ImageRoute + dish.ImageKey : ""
            };
        }
    }
}