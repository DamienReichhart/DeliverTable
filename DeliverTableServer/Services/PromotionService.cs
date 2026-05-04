using DeliverTableServer.Common;
using DeliverTableServer.Constants;
using DeliverTableInfrastructure.Extensions;
using DeliverTableServer.Extensions;
using DeliverTableServer.Helpers;
using DeliverTableServer.Mappers;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Dish;
using DeliverTableSharedLibrary.Dtos.Promotion;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableServer.Services;

public sealed class PromotionService(
    IPromotionRepository promotionRepository,
    IRestaurantRepository restaurantRepository,
    IDishRepository dishRepository
) : IPromotionService
{
    private readonly IPromotionRepository _promotionRepository = promotionRepository;
    private readonly IRestaurantRepository _restaurantRepository = restaurantRepository;
    private readonly IDishRepository _dishRepository = dishRepository;

    public async Task<ServiceResult<PromotionDto>> CreateAsync(
        int restaurantId, int ownerId, CreatePromotionRequest request, CancellationToken ct = default)
    {
        var ownershipResult = await RestaurantValidationHelper.ValidateOwnershipAsync(
            _restaurantRepository, restaurantId, ownerId, ct);
        if (!ownershipResult.IsSuccess)
            return ownershipResult.Error!;

        if (!Enum.TryParse<PromotionType>(request.PromotionType, ignoreCase: true, out var promotionType))
            return new ServiceError(ErrorMessages.InvalidFields);

        var discountError = DiscountValidationHelper.ValidateDiscountType(
            request.DiscountType, request.DiscountValue, out var discountType);
        if (discountError is not null) return discountError;

        var dateError = DiscountValidationHelper.ValidateDateRange(request.StartsAt, request.EndsAt);
        if (dateError is not null) return dateError;

        if (promotionType == PromotionType.ItemBased && request.DishIds.Count > 0)
        {
            var (restaurantDishes, _) = await _dishRepository.GetByRestaurantIdAsync(new DishQuery(), restaurantId, ct);
            var restaurantDishIds = restaurantDishes.Select(d => d.Id).ToHashSet();
            if (request.DishIds.Any(id => !restaurantDishIds.Contains(id)))
                return new ServiceError(ErrorMessages.PromotionDishNotFromRestaurant);
        }

        var promotion = new Promotion
        {
            RestaurantId = restaurantId,
            Name = request.Name,
            Description = request.Description,
            PromotionType = promotionType,
            DiscountType = discountType,
            DiscountValue = request.DiscountValue,
            MinOrderAmount = request.MinOrderAmount,
            StartsAt = request.StartsAt.ToUtc(),
            EndsAt = request.EndsAt.ToUtc(),
            PromotionDishes = request.DishIds.Select(id => new PromotionDish { DishId = id }).ToList()
        };

        var created = await _promotionRepository.CreateAsync(promotion, ct);
        return created.ToDto();
    }

    public async Task<ServiceResult<PaginatedResult<PromotionDto>>> GetByRestaurantAsync(
        int restaurantId, int ownerId, PromotionQuery query, CancellationToken ct = default)
    {
        var ownershipResult = await RestaurantValidationHelper.ValidateOwnershipAsync(
            _restaurantRepository, restaurantId, ownerId, ct);
        if (!ownershipResult.IsSuccess)
            return ownershipResult.Error!;

        var data = await _promotionRepository.GetByRestaurantAsync(restaurantId, query, ct);
        return data.ToPaginatedResult(p => p.ToDto(), query.PageNumber, query.PageSize);
    }

    public async Task<ServiceResult<PromotionDto>> UpdateAsync(
        int promotionId, int ownerId, UpdatePromotionRequest request, CancellationToken ct = default)
    {
        var promotion = await _promotionRepository.GetByIdAsync(promotionId, ct);
        if (promotion is null)
            return ServiceError.NotFound(ErrorMessages.PromotionNotFound);

        var ownershipResult = await RestaurantValidationHelper.ValidateOwnershipAsync(
            _restaurantRepository, promotion.RestaurantId, ownerId, ct);
        if (!ownershipResult.IsSuccess)
            return ownershipResult.Error!;
        var restaurant = ownershipResult.Value!;

        if (!Enum.TryParse<PromotionType>(request.PromotionType, ignoreCase: true, out var promotionType))
            return new ServiceError(ErrorMessages.InvalidFields);

        var discountError = DiscountValidationHelper.ValidateDiscountType(
            request.DiscountType, request.DiscountValue, out var discountType);
        if (discountError is not null) return discountError;

        var dateError = DiscountValidationHelper.ValidateDateRange(request.StartsAt, request.EndsAt);
        if (dateError is not null) return dateError;

        if (promotionType == PromotionType.ItemBased && request.DishIds.Count > 0)
        {
            var (restaurantDishes, _) = await _dishRepository.GetByRestaurantIdAsync(new DishQuery(), restaurant.Id, ct);
            var restaurantDishIds = restaurantDishes.Select(d => d.Id).ToHashSet();
            if (request.DishIds.Any(id => !restaurantDishIds.Contains(id)))
                return new ServiceError(ErrorMessages.PromotionDishNotFromRestaurant);
        }

        promotion.Name = request.Name;
        promotion.Description = request.Description;
        promotion.PromotionType = promotionType;
        promotion.DiscountType = discountType;
        promotion.DiscountValue = request.DiscountValue;
        promotion.MinOrderAmount = request.MinOrderAmount;
        promotion.StartsAt = request.StartsAt.ToUtc();
        promotion.EndsAt = request.EndsAt.ToUtc();
        promotion.IsActive = request.IsActive;
        promotion.PromotionDishes.Clear();
        promotion.PromotionDishes.AddRange(request.DishIds.Select(id => new PromotionDish { DishId = id }));

        var updated = await _promotionRepository.UpdateAsync(promotion, ct);
        return updated.ToDto();
    }

    public async Task<ServiceResult> DeleteAsync(int promotionId, int ownerId, CancellationToken ct = default)
    {
        var promotion = await _promotionRepository.GetByIdAsync(promotionId, ct);
        if (promotion is null)
            return ServiceError.NotFound(ErrorMessages.PromotionNotFound);

        var ownershipResult = await RestaurantValidationHelper.ValidateOwnershipAsync(
            _restaurantRepository, promotion.RestaurantId, ownerId, ct);
        if (!ownershipResult.IsSuccess)
            return ownershipResult.Error!;

        await _promotionRepository.DeleteAsync(promotionId, ct);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult<List<PromotionDto>>> GetActiveByRestaurantAsync(
        int restaurantId, CancellationToken ct = default)
    {
        var promotions = await _promotionRepository.GetActiveByRestaurantAsync(restaurantId, ct);
        return promotions.Select(p => p.ToDto()).ToList();
    }
}
