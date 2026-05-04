using DeliverTableServer.Common;
using DeliverTableServer.Constants;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;

namespace DeliverTableServer.Helpers;

public static class RestaurantValidationHelper
{
    public static async Task<ServiceResult<Restaurant>> ValidateOwnershipAsync(
        IRestaurantRepository repository, int restaurantId, int ownerId, CancellationToken ct)
    {
        var restaurant = await repository.GetByIdAsync(restaurantId, ct);
        if (restaurant is null || restaurant.OwnerId != ownerId)
            return ServiceError.NotFound(ErrorMessages.RestaurantNotFound);
        return restaurant;
    }
}
