using DeliverTableServer.Common;
using DeliverTableServer.Constants;
using DeliverTableServer.Mappers;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableServer.Services;

public sealed class AdminDishService(IDishRepository dishRepository, IRestaurantRepository restaurantRepository)
    : IAdminDishService
{
    private readonly IDishRepository _dishRepository = dishRepository;
    private readonly IRestaurantRepository _restaurantRepository = restaurantRepository;

    public async Task<ServiceResult<List<AdminDishResponse>>> GetAllAsync(CancellationToken ct = default)
    {
        var dishes = await _dishRepository.GetAllUnscopedAsync(ct);
        var result = dishes.Select(d => d.ToAdminDto()).ToList();
        return result;
    }

    public async Task<ServiceResult<AdminDishResponse>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var dish = await _dishRepository.GetByIdWithRestaurantAsync(id, ct);
        if (dish is null)
            return new ServiceError(ErrorMessages.DishNotFound, 404);

        return dish.ToAdminDto();
    }

    public async Task<ServiceResult<AdminDishResponse>> CreateAsync(
        AdminCreateDishRequest request, CancellationToken ct = default)
    {
        var restaurant = await _restaurantRepository.GetByIdAsync(request.RestaurantId, ct);
        if (restaurant is null)
            return new ServiceError(ErrorMessages.RestaurantNotFound, 404);

        var dish = new Dish
        {
            Name = request.Name,
            Description = request.Description ?? "",
            BasePrice = request.BasePrice,
            RestaurantId = request.RestaurantId,
            IsVegetarian = request.IsVegetarian,
            IsVegan = request.IsVegan,
            IsGlutenFree = request.IsGlutenFree,
            IsAllergenHazard = request.IsAllergenHazard,
            IsDishOfTheDay = request.IsDishOfTheDay,
            IsActive = request.IsActive
        };

        var created = await _dishRepository.CreateAsync(dish, ct);
        return created.ToAdminDto();
    }

    public async Task<ServiceResult<AdminDishResponse>> UpdateAsync(
        int id, AdminUpdateDishRequest request, CancellationToken ct = default)
    {
        var dish = await _dishRepository.GetByIdWithRestaurantAsync(id, ct);
        if (dish is null)
            return new ServiceError(ErrorMessages.DishNotFound, 404);

        dish.Name = request.Name;
        dish.Description = request.Description ?? "";
        dish.BasePrice = request.BasePrice;
        dish.IsVegetarian = request.IsVegetarian;
        dish.IsVegan = request.IsVegan;
        dish.IsGlutenFree = request.IsGlutenFree;
        dish.IsAllergenHazard = request.IsAllergenHazard;
        dish.IsDishOfTheDay = request.IsDishOfTheDay;
        dish.IsActive = request.IsActive;
        dish.UpdatedAt = DateTime.UtcNow;

        var updated = await _dishRepository.UpdateAsync(dish, ct);
        return updated.ToAdminDto();
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken ct = default)
    {
        var deleted = await _dishRepository.DeleteAsync(id, ct);
        if (!deleted)
            return new ServiceError(ErrorMessages.DishNotFound, 404);

        return ServiceResult.Success();
    }
}
