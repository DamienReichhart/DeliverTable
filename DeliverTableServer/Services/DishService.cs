using DeliverTableServer.Common;
using DeliverTableServer.Configuration;
using DeliverTableServer.Constants;
using DeliverTableServer.Extensions;
using DeliverTableServer.Mappers;
using DeliverTableServer.Models;
using DeliverTableServer.Repositories.Interfaces;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Dish;

namespace DeliverTableServer.Services;

public sealed class DishService(
    IDishRepository dishRepository,
    IObjectStorageService objectStorage,
    AppEnvironment appEnvironment
) : IDishService
{
    private readonly IDishRepository _dishRepository = dishRepository;
    private readonly IObjectStorageService _objectStorage = objectStorage;
    private readonly long _maxUploadBytes = UploadLimits.ToBytes(appEnvironment.UploadMaxSizeMb);
    private readonly int _maxUploadMb = appEnvironment.UploadMaxSizeMb;
    private const string DishImageFolder = "images/dish";

    public async Task<ServiceResult<PaginatedResult<DishDto>>> GetAllAsync(DishQuery query, CancellationToken ct = default)
    {
        var data = await _dishRepository.GetAllAsync(query, ct);
        return data.ToPaginatedResult(d => d.ToDto(), 1, data.TotalCount);
    }

    public async Task<ServiceResult<DishDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var dish = await _dishRepository.GetByIdAsync(id, ct);
        if (dish is null)
            return new ServiceError(ErrorMessages.DishNotFound, 404);

        return dish.ToDto();
    }

    public async Task<ServiceResult<PaginatedResult<DishDto>>> GetByRestaurantIdAsync(
        DishQuery query, int restaurantId, CancellationToken ct = default)
    {
        var data = await _dishRepository.GetByRestaurantIdAsync(query, restaurantId, ct);
        return data.ToPaginatedResult(d => d.ToDto(), 1, data.TotalCount);
    }

    public async Task<ServiceResult<DishDto>> CreateAsync(
        CreateDishDto dto, int restaurantId, IFormFile? image, CancellationToken ct = default)
    {
        if (image is not null && image.Length > _maxUploadBytes)
            return new ServiceError(ErrorMessages.FileTooLarge(_maxUploadMb), 413);

        var dish = new Dish
        {
            Name = dto.Name,
            Description = dto.Description,
            BasePrice = dto.BasePrice,
            IsVegetarian = dto.IsVegetarian,
            IsVegan = dto.IsVegan,
            IsGlutenFree = dto.IsGlutenFree,
            IsAllergenHazard = dto.IsAllergenHazard,
            IsDishOfTheDay = dto.IsDishOfTheDay,
            RestaurantId = restaurantId
        };

        var created = await _dishRepository.CreateAsync(dish, ct);

        if (image is not null)
            await _objectStorage.UploadAsync(image, DishImageFolder, created.Id);

        return created.ToDto();
    }

    public async Task<ServiceResult<DishDto>> UpdateAsync(
        int id, CreateDishDto dto, IFormFile? image, CancellationToken ct = default)
    {
        if (image is not null && image.Length > _maxUploadBytes)
            return new ServiceError(ErrorMessages.FileTooLarge(_maxUploadMb), 413);

        var dish = await _dishRepository.GetByIdAsync(id, ct);
        if (dish is null)
            return new ServiceError(ErrorMessages.DishNotFound, 404);

        if (image is not null)
        {
            await _objectStorage.DeleteAsync($"{DishImageFolder}/{dish.Id}");
            await _objectStorage.UploadAsync(image, DishImageFolder, dish.Id);
        }

        dish.Name = dto.Name;
        dish.Description = dto.Description;
        dish.BasePrice = dto.BasePrice;
        dish.IsVegetarian = dto.IsVegetarian;
        dish.IsVegan = dto.IsVegan;
        dish.IsGlutenFree = dto.IsGlutenFree;
        dish.IsAllergenHazard = dto.IsAllergenHazard;
        dish.IsDishOfTheDay = dto.IsDishOfTheDay;

        var updated = await _dishRepository.UpdateAsync(dish, ct);
        return updated.ToDto();
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken ct = default)
    {
        var dish = await _dishRepository.GetByIdAsync(id, ct);
        if (dish is null)
            return new ServiceError(ErrorMessages.DishNotFound, 404);

        await _objectStorage.DeleteAsync($"dish/{dish.Id}");
        var deleted = await _dishRepository.DeleteAsync(id, ct);
        if (!deleted)
            return new ServiceError(ErrorMessages.DishNotFound, 404);

        return ServiceResult.Success();
    }
}
