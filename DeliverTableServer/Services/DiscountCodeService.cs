using DeliverTableServer.Common;
using DeliverTableServer.Constants;
using DeliverTableServer.Mappers;
using DeliverTableServer.Repositories.Interfaces;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.DiscountCode;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableServer.Services;

public sealed class DiscountCodeService(
    IDiscountCodeRepository discountCodeRepository,
    IRestaurantRepository restaurantRepository
) : IDiscountCodeService
{
    private readonly IDiscountCodeRepository _discountCodeRepository = discountCodeRepository;
    private readonly IRestaurantRepository _restaurantRepository = restaurantRepository;

    public async Task<ServiceResult<DiscountCodeDto>> CreateAsync(
        int restaurantId, int ownerId, CreateDiscountCodeRequest request, CancellationToken ct = default)
    {
        var restaurant = await _restaurantRepository.GetByIdAsync(restaurantId, ct);
        if (restaurant is null)
            return new ServiceError(ErrorMessages.RestaurantNotFound, 404);

        if (restaurant.OwnerId != ownerId)
            return new ServiceError(ErrorMessages.RestaurantNotFound, 404);

        if (!Enum.TryParse<DiscountType>(request.DiscountType, ignoreCase: true, out var discountType))
            return new ServiceError(ErrorMessages.InvalidFields);

        if (request.ValidUntil <= request.ValidFrom)
            return new ServiceError(ErrorMessages.InvalidPromotionDates);

        var existing = await _discountCodeRepository.GetByCodeAndRestaurantAsync(request.Code, restaurantId, ct);
        if (existing is not null)
            return new ServiceError(ErrorMessages.DiscountCodeAlreadyExists);

        var entity = new Models.DiscountCode
        {
            RestaurantId = restaurantId,
            Code = request.Code,
            Description = request.Description,
            DiscountType = discountType,
            DiscountValue = request.DiscountValue,
            MinOrderAmount = request.MinOrderAmount,
            ValidFrom = DateTime.SpecifyKind(request.ValidFrom, DateTimeKind.Utc),
            ValidUntil = DateTime.SpecifyKind(request.ValidUntil, DateTimeKind.Utc),
            MaxRedemptions = request.MaxRedemptions,
            PerUserLimit = request.PerUserLimit
        };

        var created = await _discountCodeRepository.CreateAsync(entity, ct);
        return created.ToDto();
    }

    public async Task<ServiceResult<PaginatedResult<DiscountCodeDto>>> GetByRestaurantAsync(
        int restaurantId, int ownerId, DiscountCodeQuery query, CancellationToken ct = default)
    {
        var restaurant = await _restaurantRepository.GetByIdAsync(restaurantId, ct);
        if (restaurant is null)
            return new ServiceError(ErrorMessages.RestaurantNotFound, 404);

        if (restaurant.OwnerId != ownerId)
            return new ServiceError(ErrorMessages.RestaurantNotFound, 404);

        var (items, totalCount) = await _discountCodeRepository.GetByRestaurantAsync(restaurantId, query, ct);

        return new PaginatedResult<DiscountCodeDto>
        {
            Items = items.Select(c => c.ToDto()).ToList(),
            TotalCount = totalCount,
            Page = query.PageNumber,
            PageSize = query.PageSize
        };
    }

    public async Task<ServiceResult<DiscountCodeDto>> UpdateAsync(
        int discountCodeId, int ownerId, UpdateDiscountCodeRequest request, CancellationToken ct = default)
    {
        var code = await _discountCodeRepository.GetByIdAsync(discountCodeId, ct);
        if (code is null)
            return new ServiceError(ErrorMessages.DiscountCodeNotFound, 404);

        var restaurant = await _restaurantRepository.GetByIdAsync(code.RestaurantId, ct);
        if (restaurant is null || restaurant.OwnerId != ownerId)
            return new ServiceError(ErrorMessages.RestaurantNotFound, 404);

        if (!Enum.TryParse<DiscountType>(request.DiscountType, ignoreCase: true, out var discountType))
            return new ServiceError(ErrorMessages.InvalidFields);

        if (request.ValidUntil <= request.ValidFrom)
            return new ServiceError(ErrorMessages.InvalidPromotionDates);

        code.Description = request.Description;
        code.DiscountType = discountType;
        code.DiscountValue = request.DiscountValue;
        code.MinOrderAmount = request.MinOrderAmount;
        code.ValidFrom = DateTime.SpecifyKind(request.ValidFrom, DateTimeKind.Utc);
        code.ValidUntil = DateTime.SpecifyKind(request.ValidUntil, DateTimeKind.Utc);
        code.MaxRedemptions = request.MaxRedemptions;
        code.PerUserLimit = request.PerUserLimit;
        code.IsActive = request.IsActive;

        var updated = await _discountCodeRepository.UpdateAsync(code, ct);
        return updated.ToDto();
    }

    public async Task<ServiceResult> DeleteAsync(int discountCodeId, int ownerId, CancellationToken ct = default)
    {
        var code = await _discountCodeRepository.GetByIdAsync(discountCodeId, ct);
        if (code is null)
            return new ServiceError(ErrorMessages.DiscountCodeNotFound, 404);

        var restaurant = await _restaurantRepository.GetByIdAsync(code.RestaurantId, ct);
        if (restaurant is null || restaurant.OwnerId != ownerId)
            return new ServiceError(ErrorMessages.RestaurantNotFound, 404);

        await _discountCodeRepository.DeleteAsync(discountCodeId, ct);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult<DiscountCodeDto>> ValidateAsync(
        int restaurantId, int customerId, string code, CancellationToken ct = default)
    {
        var discountCode = await _discountCodeRepository.GetByCodeAndRestaurantAsync(code, restaurantId, ct);

        var now = DateTime.UtcNow;
        if (discountCode is null || !discountCode.IsActive ||
            now < discountCode.ValidFrom || now > discountCode.ValidUntil)
            return new ServiceError(ErrorMessages.DiscountCodeInvalid);

        if (discountCode.MaxRedemptions.HasValue &&
            discountCode.CurrentRedemptions >= discountCode.MaxRedemptions.Value)
            return new ServiceError(ErrorMessages.DiscountCodeMaxRedemptions);

        var userRedemptions = await _discountCodeRepository.GetRedemptionCountByUserAsync(
            discountCode.Id, customerId, ct);
        if (userRedemptions >= discountCode.PerUserLimit)
            return new ServiceError(ErrorMessages.DiscountCodePerUserLimit);

        return discountCode.ToDto();
    }
}
