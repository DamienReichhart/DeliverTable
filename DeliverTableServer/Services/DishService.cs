using DeliverTableServer.Common;
using DeliverTableServer.Configuration;
using DeliverTableServer.Constants;
using DeliverTableInfrastructure.Extensions;
using DeliverTableServer.Extensions;
using DeliverTableServer.Mappers;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableInfrastructure.Services.Interfaces;
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
        (List<Dish> Items, int TotalCount) data = await _dishRepository.GetAllAsync(query, ct);
        return data.ToPaginatedResult(d => d.ToDto(), 1, data.TotalCount);
    }

    public async Task<ServiceResult<DishDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        Dish? dish = await _dishRepository.GetByIdAsync(id, ct);
        if (dish is null)
            return ServiceError.NotFound(ErrorMessages.DishNotFound);

        return dish.ToDto();
    }

    public async Task<ServiceResult<PaginatedResult<DishDto>>> GetByRestaurantIdAsync(
        DishQuery query, int restaurantId, CancellationToken ct = default)
    {
        (List<Dish> Items, int TotalCount) data = await _dishRepository.GetByRestaurantIdAsync(query, restaurantId, ct);
        return data.ToPaginatedResult(d => d.ToDto(), 1, data.TotalCount);
    }

    public async Task<ServiceResult<DishDto>> CreateAsync(
        CreateDishDto dto, int restaurantId, IFormFile? image, CancellationToken ct = default)
    {
        ServiceError? imageError = ValidateImage(image);
        if (imageError is not null)
            return imageError;

        Dish dish = new Dish { RestaurantId = restaurantId };
        ApplyDtoToDish(dto, dish);

        Dish created = await _dishRepository.CreateAsync(dish, ct);

        if (image is not null)
            await _objectStorage.UploadAsync(image, DishImageFolder, created.Id);

        return created.ToDto();
    }

    public async Task<ServiceResult<DishDto>> UpdateAsync(
        int id, CreateDishDto dto, IFormFile? image, CancellationToken ct = default)
    {
        ServiceError? imageError = ValidateImage(image);
        if (imageError is not null)
            return imageError;

        Dish? dish = await _dishRepository.GetByIdAsync(id, ct);
        if (dish is null)
            return ServiceError.NotFound(ErrorMessages.DishNotFound);

        if (image is not null)
        {
            await _objectStorage.DeleteAsync($"{DishImageFolder}/{dish.Id}");
            await _objectStorage.UploadAsync(image, DishImageFolder, dish.Id);
        }

        ApplyDtoToDish(dto, dish);

        Dish updated = await _dishRepository.UpdateAsync(dish, ct);
        return updated.ToDto();
    }

    private ServiceError? ValidateImage(IFormFile? image) =>
        image is not null && image.Length > _maxUploadBytes
            ? new ServiceError(ErrorMessages.FileTooLarge(_maxUploadMb), 413)
            : null;

    private static void ApplyDtoToDish(CreateDishDto dto, Dish dish)
    {
        dish.Name = dto.Name;
        dish.Description = dto.Description;
        dish.BasePrice = dto.BasePrice;
        dish.IsVegetarian = dto.IsVegetarian;
        dish.IsVegan = dto.IsVegan;
        dish.IsGlutenFree = dto.IsGlutenFree;
        dish.IsAllergenHazard = dto.IsAllergenHazard;
        dish.IsDishOfTheDay = dto.IsDishOfTheDay;
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken ct = default)
    {
        Dish? dish = await _dishRepository.GetByIdAsync(id, ct);
        if (dish is null)
            return ServiceError.NotFound(ErrorMessages.DishNotFound);

        await _objectStorage.DeleteAsync($"{DishImageFolder}/{dish.Id}");
        bool deleted = await _dishRepository.DeleteAsync(id, ct);
        if (!deleted)
            return ServiceError.NotFound(ErrorMessages.DishNotFound);

        return ServiceResult.Success();
    }
}
