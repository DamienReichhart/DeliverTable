using DeliverTableServer.Common;
using DeliverTableServer.Constants;
using DeliverTableServer.Mappers;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Admin;
using DeliverTableInfrastructure.Models;

namespace DeliverTableServer.Services;

public sealed class AdminRestaurantService(IRestaurantRepository restaurantRepository) : IAdminRestaurantService
{
    private readonly IRestaurantRepository _restaurantRepository = restaurantRepository;

    public async Task<ServiceResult<List<AdminRestaurantResponse>>> GetAllAsync(CancellationToken ct = default)
    {
        List<Restaurant> restaurants = await _restaurantRepository.GetAllUnscopedAsync(ct);
        List<AdminRestaurantResponse> result = restaurants.Select(r => r.ToAdminDto()).ToList();
        return result;
    }

    public async Task<ServiceResult<AdminRestaurantResponse>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        Restaurant? restaurant = await _restaurantRepository.GetByIdWithOwnerAsync(id, ct);
        if (restaurant is null)
            return ServiceError.NotFound(ErrorMessages.RestaurantNotFound);

        return restaurant.ToAdminDto();
    }

    public async Task<ServiceResult<AdminRestaurantResponse>> UpdateAsync(
        int id, AdminUpdateRestaurantRequest request, CancellationToken ct = default)
    {
        Restaurant? restaurant = await _restaurantRepository.GetByIdWithOwnerAsync(id, ct);
        if (restaurant is null)
            return ServiceError.NotFound(ErrorMessages.RestaurantNotFound);

        restaurant.Name = request.Name;
        restaurant.Description = request.Description ?? "";
        restaurant.AdressLine1 = request.AdressLine1;
        restaurant.AdressLine2 = request.AdressLine2 ?? "";
        restaurant.City = request.City;
        restaurant.ZipCode = request.ZipCode;
        restaurant.Country = request.Country;
        restaurant.IsActive = request.IsActive;
        restaurant.UpdatedAt = DateTime.UtcNow;

        Restaurant updated = await _restaurantRepository.UpdateAsync(restaurant, ct);
        return updated.ToAdminDto();
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken ct = default)
    {
        bool deleted = await _restaurantRepository.DeleteAsync(id, ct);
        if (!deleted)
            return ServiceError.NotFound(ErrorMessages.RestaurantNotFound);

        return ServiceResult.Success();
    }
}
