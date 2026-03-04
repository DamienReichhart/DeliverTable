using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DeliverTableSharedLibrary.Dtos.Dish;
using DeliverTableServer.Models;
namespace DeliverTableServer.Repositories.Interfaces
{
    public interface IDishRepository
    {
        Task<List<Dish>> GetAllDishes(DishQuery query);
        Task<List<Dish>> GetDishesByRestaurantId(DishQuery query, int restaurantId);
        Task<Dish?> GetDishById(int id);
        Task<Dish> CreateDish(CreateDishDto createDishDto, int restaurantId, IFormFile? image);
        Task<Dish> UpdateDish(int id, CreateDishDto createDishDto, IFormFile? image);
        Task DeleteDish(int id);
    }
}