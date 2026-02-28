using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DeliverTableSharedLibrary.Dtos.Restaurant;

namespace DeliverTableClient.Services.Interfaces
{
    public interface IRestaurantService
    {
        Task<bool> CreateRestaurant(CreateRestaurantDto creationDto, CancellationToken cancellationToken = default);
    }
}