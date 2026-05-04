using DeliverTableServer.Common;
using DeliverTableServer.Constants;
using DeliverTableServer.Mappers;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Admin;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableServer.Services;

public sealed class AdminPromotionService(IPromotionRepository promotionRepository, IRestaurantRepository restaurantRepository)
    : IAdminPromotionService
{
    private readonly IPromotionRepository _promotionRepository = promotionRepository;
    private readonly IRestaurantRepository _restaurantRepository = restaurantRepository;

    public async Task<ServiceResult<List<AdminPromotionResponse>>> GetAllAsync(CancellationToken ct = default)
    {
        var promotions = await _promotionRepository.GetAllUnscopedAsync(ct);
        var result = promotions.Select(p => p.ToAdminDto()).ToList();
        return result;
    }

    public async Task<ServiceResult<AdminPromotionResponse>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var promotion = await _promotionRepository.GetByIdWithRestaurantAsync(id, ct);
        if (promotion is null)
            return ServiceError.NotFound(ErrorMessages.PromotionNotFound);

        return promotion.ToAdminDto();
    }

    public async Task<ServiceResult<AdminPromotionResponse>> CreateAsync(
        AdminCreatePromotionRequest request, CancellationToken ct = default)
    {
        var restaurant = await _restaurantRepository.GetByIdAsync(request.RestaurantId, ct);
        if (restaurant is null)
            return ServiceError.NotFound(ErrorMessages.RestaurantNotFound);

        if (request.EndsAt <= request.StartsAt)
            return ServiceError.BadRequest(ErrorMessages.InvalidPromotionDates);

        if (request.DiscountType == DiscountType.Percentage && request.DiscountValue > 100)
            return ServiceError.BadRequest(ErrorMessages.PercentageDiscountTooHigh);

        var promotion = new Promotion
        {
            Name = request.Name,
            Description = request.Description ?? "",
            PromotionType = request.PromotionType,
            DiscountType = request.DiscountType,
            DiscountValue = request.DiscountValue,
            MinOrderAmount = request.MinOrderAmount,
            StartsAt = request.StartsAt,
            EndsAt = request.EndsAt,
            RestaurantId = request.RestaurantId,
            IsActive = request.IsActive
        };

        var created = await _promotionRepository.CreateAsync(promotion, ct);
        return created.ToAdminDto();
    }

    public async Task<ServiceResult<AdminPromotionResponse>> UpdateAsync(
        int id, AdminUpdatePromotionRequest request, CancellationToken ct = default)
    {
        var promotion = await _promotionRepository.GetByIdWithRestaurantAsync(id, ct);
        if (promotion is null)
            return ServiceError.NotFound(ErrorMessages.PromotionNotFound);

        if (request.EndsAt <= request.StartsAt)
            return ServiceError.BadRequest(ErrorMessages.InvalidPromotionDates);

        if (request.DiscountType == DiscountType.Percentage && request.DiscountValue > 100)
            return ServiceError.BadRequest(ErrorMessages.PercentageDiscountTooHigh);

        promotion.Name = request.Name;
        promotion.Description = request.Description ?? "";
        promotion.PromotionType = request.PromotionType;
        promotion.DiscountType = request.DiscountType;
        promotion.DiscountValue = request.DiscountValue;
        promotion.MinOrderAmount = request.MinOrderAmount;
        promotion.StartsAt = request.StartsAt;
        promotion.EndsAt = request.EndsAt;
        promotion.IsActive = request.IsActive;
        promotion.UpdatedAt = DateTime.UtcNow;

        var updated = await _promotionRepository.UpdateAsync(promotion, ct);
        return updated.ToAdminDto();
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken ct = default)
    {
        var deleted = await _promotionRepository.DeleteAsync(id, ct);
        if (!deleted)
            return ServiceError.NotFound(ErrorMessages.PromotionNotFound);

        return ServiceResult.Success();
    }
}
