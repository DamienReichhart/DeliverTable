using DeliverTableServer.Common;
using DeliverTableServer.Constants;
using DeliverTableServer.Mappers;
using DeliverTableServer.Models;
using DeliverTableServer.Repositories.Interfaces;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos;
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
        var restaurant = await _restaurantRepository.GetByIdAsync(restaurantId, ct);
        if (restaurant is null)
            return new ServiceError(ErrorMessages.RestaurantNotFound, 404);

        if (restaurant.OwnerId != ownerId)
            return new ServiceError(ErrorMessages.RestaurantNotFound, 404);

        if (!Enum.TryParse<PromotionType>(request.PromotionType, ignoreCase: true, out var promotionType))
            return new ServiceError(ErrorMessages.InvalidFields);

        if (!Enum.TryParse<DiscountType>(request.DiscountType, ignoreCase: true, out var discountType))
            return new ServiceError(ErrorMessages.InvalidFields);

        if (request.EndsAt <= request.StartsAt)
            return new ServiceError(ErrorMessages.InvalidPromotionDates);

        if (promotionType == PromotionType.ItemBased)
        {
            foreach (var dishId in request.DishIds)
            {
                var dish = await _dishRepository.GetByIdAsync(dishId, ct);
                if (dish is null || dish.RestaurantId != restaurantId)
                    return new ServiceError(ErrorMessages.PromotionDishNotFromRestaurant);
            }
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
            StartsAt = DateTime.SpecifyKind(request.StartsAt, DateTimeKind.Utc),
            EndsAt = DateTime.SpecifyKind(request.EndsAt, DateTimeKind.Utc),
            PromotionDishes = request.DishIds.Select(id => new PromotionDish { DishId = id }).ToList()
        };

        var created = await _promotionRepository.CreateAsync(promotion, ct);
        return created.ToDto();
    }

    public async Task<ServiceResult<PaginatedResult<PromotionDto>>> GetByRestaurantAsync(
        int restaurantId, int ownerId, PromotionQuery query, CancellationToken ct = default)
    {
        var restaurant = await _restaurantRepository.GetByIdAsync(restaurantId, ct);
        if (restaurant is null)
            return new ServiceError(ErrorMessages.RestaurantNotFound, 404);

        if (restaurant.OwnerId != ownerId)
            return new ServiceError(ErrorMessages.RestaurantNotFound, 404);

        var (items, totalCount) = await _promotionRepository.GetByRestaurantAsync(restaurantId, query, ct);

        return new PaginatedResult<PromotionDto>
        {
            Items = items.Select(p => p.ToDto()).ToList(),
            TotalCount = totalCount,
            Page = query.PageNumber,
            PageSize = query.PageSize
        };
    }

    public async Task<ServiceResult<PromotionDto>> UpdateAsync(
        int promotionId, int ownerId, UpdatePromotionRequest request, CancellationToken ct = default)
    {
        var promotion = await _promotionRepository.GetByIdAsync(promotionId, ct);
        if (promotion is null)
            return new ServiceError(ErrorMessages.PromotionNotFound, 404);

        var restaurant = await _restaurantRepository.GetByIdAsync(promotion.RestaurantId, ct);
        if (restaurant is null || restaurant.OwnerId != ownerId)
            return new ServiceError(ErrorMessages.RestaurantNotFound, 404);

        if (!Enum.TryParse<PromotionType>(request.PromotionType, ignoreCase: true, out var promotionType))
            return new ServiceError(ErrorMessages.InvalidFields);

        if (!Enum.TryParse<DiscountType>(request.DiscountType, ignoreCase: true, out var discountType))
            return new ServiceError(ErrorMessages.InvalidFields);

        if (request.EndsAt <= request.StartsAt)
            return new ServiceError(ErrorMessages.InvalidPromotionDates);

        if (promotionType == PromotionType.ItemBased)
        {
            foreach (var dishId in request.DishIds)
            {
                var dish = await _dishRepository.GetByIdAsync(dishId, ct);
                if (dish is null || dish.RestaurantId != restaurant.Id)
                    return new ServiceError(ErrorMessages.PromotionDishNotFromRestaurant);
            }
        }

        promotion.Name = request.Name;
        promotion.Description = request.Description;
        promotion.PromotionType = promotionType;
        promotion.DiscountType = discountType;
        promotion.DiscountValue = request.DiscountValue;
        promotion.MinOrderAmount = request.MinOrderAmount;
        promotion.StartsAt = DateTime.SpecifyKind(request.StartsAt, DateTimeKind.Utc);
        promotion.EndsAt = DateTime.SpecifyKind(request.EndsAt, DateTimeKind.Utc);
        promotion.IsActive = request.IsActive;
        promotion.PromotionDishes = request.DishIds.Select(id => new PromotionDish { DishId = id }).ToList();

        var updated = await _promotionRepository.UpdateAsync(promotion, ct);
        return updated.ToDto();
    }

    public async Task<ServiceResult> DeleteAsync(int promotionId, int ownerId, CancellationToken ct = default)
    {
        var promotion = await _promotionRepository.GetByIdAsync(promotionId, ct);
        if (promotion is null)
            return new ServiceError(ErrorMessages.PromotionNotFound, 404);

        var restaurant = await _restaurantRepository.GetByIdAsync(promotion.RestaurantId, ct);
        if (restaurant is null || restaurant.OwnerId != ownerId)
            return new ServiceError(ErrorMessages.RestaurantNotFound, 404);

        await _promotionRepository.DeleteAsync(promotionId, ct);
        return ServiceResult.Success();
    }
}
