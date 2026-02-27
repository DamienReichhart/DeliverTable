using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DeliverTableServer.Models;
using DeliverTableSharedLibrary.Dtos.Restaurant;
using Microsoft.AspNetCore.Mvc;

namespace DeliverTableServer.Repositories.Interfaces
{
    public interface IRestaurantRepository
    {
        Task<Restaurant> CreateRestaurant(
            CreateRestaurantDto creationDto,
            int ownerId,
            double lon,
            double lat
        );

        Task<List<Restaurant>> GetAllRestaurant(RestaurantQuery query);

        Task<Restaurant?> GetRestaurantById(int id);

        Task<bool> Delete(int id);

        Task<Restaurant> Update(
            int id,
            UpdateRestaurantDto restaurantDto,
            double lon,
            double lat
        );

        Task<List<Restaurant>> GetRestaurantByOwner(int id, RestaurantQuery query);
    }
}