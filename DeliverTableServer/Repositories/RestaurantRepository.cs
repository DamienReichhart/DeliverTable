using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DeliverTableServer.Data;
using DeliverTableServer.Models;
using DeliverTableServer.Repositories.Interfaces;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos.Restaurant;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableServer.Repositories
{
    public class RestaurantRepository(
        DeliverTableContext dbContext
    ) : IRestaurantRepository
    {
        private readonly DeliverTableContext _dbContext = dbContext;

        public async Task<Restaurant> CreateRestaurant(
            CreateRestaurantDto creationDto,
            int ownerId,
            double lon,
            double lat
        )
        {
            _ = Enum.TryParse<RestaurantType>(creationDto.Type, out var restaurantType);
            Restaurant restaurant = new()
            {
                Name = creationDto.Name,
                Description = creationDto.Description ?? string.Empty,
                AdressLine1 = creationDto.AdressLine1,
                City = creationDto.City,
                ZipCode = creationDto.ZipCode,
                Type = restaurantType,
                Country = char.ToUpper(creationDto.Country[0]) + creationDto.Country.Substring(1),
                AdressLine2 = creationDto.AdressLine2 ?? string.Empty,
                OwnerId = ownerId,
                Longitude = lon,
                Latitude = lat,
            };

            _dbContext.Restaurants.Add(restaurant);

            await _dbContext.SaveChangesAsync();

            return restaurant;
        }

        public async Task<bool> Delete(int id)
        {
            Restaurant? restaurant = await _dbContext.Restaurants.FirstOrDefaultAsync(r => r.Id == id);
            if (restaurant == null)
            {
                return false;
            }
            _dbContext.Restaurants.Remove(restaurant);
            await _dbContext.SaveChangesAsync();

            return true;
        }

        public async Task<List<Restaurant>> GetAllRestaurant(RestaurantQuery query)
        {
            var restaurants = _dbContext.Restaurants.AsQueryable();
            if (!string.IsNullOrWhiteSpace(query.Name))
            {
                restaurants = restaurants.Where(r => r.Name.Contains(query.Name));
            }
            if (!string.IsNullOrWhiteSpace(query.City))
            {
                restaurants = restaurants.Where(r => r.City.Contains(query.City));
            }
            if (!string.IsNullOrWhiteSpace(query.Type))
            {
                restaurants = restaurants.Where(r => r.Type.ToString().Contains(query.Type));
            }

            restaurants = restaurants.OrderBy(r => r.Id);

            int skipNumber = (query.PageNumber - 1) * query.PageSize;

            List<Restaurant> result = await restaurants.Skip(skipNumber).Take(query.PageSize).ToListAsync();

            return result;
        }

        public async Task<object?> GetRestaurantById(int id)
        {
            return await _dbContext.Restaurants
            .Where(r => r.Id == id)
            .Select(r => new
            {
                r.Id,
                r.Name,
                Type = r.Type.ToString(),
                r.Description,
                r.AdressLine1,
                r.AdressLine2,
                r.City,
                r.ZipCode,
                r.Country,
                r.Latitude,
                r.Longitude,
                r.IsActive,
                Owner = $"{r.Owner.FirstName} {r.Owner.LastName}"
            })
            .FirstOrDefaultAsync();
        }

        public async Task<Restaurant> Update(int id, UpdateRestaurantDto restaurantDto, double lon, double lat)
        {
            var restaurant = await _dbContext.Restaurants
                .FirstOrDefaultAsync(r => r.Id == id) ?? throw new ArgumentException("Restaurant non trouvé");
            bool isValid = Enum.TryParse<RestaurantType>(restaurantDto.Type, out var restaurantType);

            if (!isValid) restaurantType = RestaurantType.Autre;

            restaurant.Name = restaurantDto.Name;
            restaurant.Description = restaurantDto.Description;
            restaurant.Type = restaurantType;
            restaurant.AdressLine1 = restaurantDto.AdressLine1;
            restaurant.AdressLine2 = restaurantDto.AdressLine2;
            restaurant.City = restaurantDto.City;
            restaurant.ZipCode = restaurantDto.ZipCode;

            restaurant.Latitude = lat;
            restaurant.Longitude = lon;

            restaurant.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            return new Restaurant
            {
                Id = restaurant.Id,
                Name = restaurant.Name,
                Type = restaurant.Type,
                Description = restaurant.Description,
                AdressLine1 = restaurant.AdressLine1,
                AdressLine2 = restaurant.AdressLine2,
                City = restaurant.City,
                ZipCode = restaurant.ZipCode,
                Country = restaurant.Country,
                Latitude = restaurant.Latitude,
                Longitude = restaurant.Longitude,
                IsActive = restaurant.IsActive
            };
        }
    }
}