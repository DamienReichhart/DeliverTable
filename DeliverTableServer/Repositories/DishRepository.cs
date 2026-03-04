using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DeliverTableServer.Data;
using DeliverTableServer.Models;
using DeliverTableServer.Repositories.Interfaces;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Dish;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableServer.Repositories
{
    public class DishRepository(DeliverTableContext context, IObjectStorageService objectStorage) : IDishRepository
    {
        private readonly DeliverTableContext _context = context;
        private readonly IObjectStorageService _objectStorage = objectStorage;

        public async Task<List<Dish>> GetAllDishes(DishQuery query)
        {
            IQueryable<Dish> dishQuery = _context.Dishes.AsQueryable();
            dishQuery = AddFiltersToQuery(dishQuery, query);
            dishQuery = dishQuery.OrderBy(d => d.Id);
            return await dishQuery.ToListAsync();
        }

        public async Task<List<Dish>> GetDishesByRestaurantId([FromQuery] DishQuery query, int restaurantId)
        {
            IQueryable<Dish> dishQuery = _context.Dishes.AsQueryable();
            dishQuery = AddFiltersToQuery(dishQuery, query);
            dishQuery = dishQuery.Where(d => d.RestaurantId == restaurantId);
            dishQuery = dishQuery.OrderBy(d => d.Id);
            return await dishQuery.ToListAsync();
        }

        public async Task<Dish?> GetDishById(int id)
        {
            return await _context.Dishes.FindAsync(id);
        }

        public async Task<Dish> CreateDish(CreateDishDto createDishDto, int restaurantId, IFormFile? image)
        {
            Dish dish = new()
            {
                Name = createDishDto.Name,
                Description = createDishDto.Description,
                BasePrice = createDishDto.BasePrice,
                IsVegetarian = createDishDto.IsVegetarian,
                IsVegan = createDishDto.IsVegan,
                IsGlutenFree = createDishDto.IsGlutenFree,
                IsAllergenHazard = createDishDto.IsAllergenHazard,
                IsDishOfTheDay = createDishDto.IsDishOfTheDay,
                RestaurantId = restaurantId,
            };

            try
            {
                if (image != null)
                {
                    dish.ImageKey = await _objectStorage.UploadAsync(image, "dish") ?? throw new ArgumentException("Image non uploadée");
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            _context.Dishes.Add(dish);
            await _context.SaveChangesAsync();

            return dish;
        }

        public async Task<Dish> UpdateDish(int id, CreateDishDto createDishDto, IFormFile? image)
        {
            Dish? dishToUpdate = await _context.Dishes.FindAsync(id) ?? throw new ArgumentException("Dish non trouvé");
            if (image != null)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(dishToUpdate.ImageKey))
                    {
                        await _objectStorage.DeleteAsync($"dish/{dishToUpdate.ImageKey}");
                    }
                    dishToUpdate.ImageKey = await _objectStorage.UploadAsync(image, "dish") ?? throw new ArgumentException("Image non uploadée");
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            }
            dishToUpdate.Name = createDishDto.Name;
            dishToUpdate.Description = createDishDto.Description;
            dishToUpdate.BasePrice = createDishDto.BasePrice;
            dishToUpdate.IsVegetarian = createDishDto.IsVegetarian;
            dishToUpdate.IsVegan = createDishDto.IsVegan;
            dishToUpdate.IsGlutenFree = createDishDto.IsGlutenFree;
            dishToUpdate.IsAllergenHazard = createDishDto.IsAllergenHazard;
            dishToUpdate.IsDishOfTheDay = createDishDto.IsDishOfTheDay;
            _context.Dishes.Update(dishToUpdate);
            await _context.SaveChangesAsync();
            return dishToUpdate;
        }

        public async Task DeleteDish(int id)
        {
            Dish? dishToDelete = await _context.Dishes.FindAsync(id) ?? throw new ArgumentException("Dish non trouvé");
            if (!string.IsNullOrWhiteSpace(dishToDelete.ImageKey))
            {
                await _objectStorage.DeleteAsync($"dish/{dishToDelete.ImageKey}");
            }
            _context.Dishes.Remove(dishToDelete);
            await _context.SaveChangesAsync();
        }

        private static IQueryable<Dish> AddFiltersToQuery(IQueryable<Dish> query, DishQuery dishQuery)
        {
            if (!string.IsNullOrWhiteSpace(dishQuery.Name))
            {
                query = query.Where(d => d.Name.Contains(dishQuery.Name));
            }
            if (dishQuery.LessThanPrice != null)
            {
                query = query.Where(d => d.BasePrice < dishQuery.LessThanPrice);
            }
            if (dishQuery.IsVegetarian != null)
            {
                query = query.Where(d => d.IsVegetarian == dishQuery.IsVegetarian);
            }
            if (dishQuery.IsVegan != null)
            {
                query = query.Where(d => d.IsVegan == dishQuery.IsVegan);
            }
            if (dishQuery.IsGlutenFree != null)
            {
                query = query.Where(d => d.IsGlutenFree == dishQuery.IsGlutenFree);
            }
            if (dishQuery.IsAllergenHazard != null)
            {
                query = query.Where(d => d.IsAllergenHazard == dishQuery.IsAllergenHazard);
            }
            if (dishQuery.IsDishOfTheDay != null)
            {
                query = query.Where(d => d.IsDishOfTheDay == dishQuery.IsDishOfTheDay);
            }
            return query;
        }
    }

}