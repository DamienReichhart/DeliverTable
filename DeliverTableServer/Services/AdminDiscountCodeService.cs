using DeliverTableServer.Common;
using DeliverTableServer.Constants;
using DeliverTableServer.Helpers;
using DeliverTableServer.Mappers;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableServer.Services;

public sealed class AdminDiscountCodeService(IDiscountCodeRepository discountCodeRepository, IRestaurantRepository restaurantRepository)
    : IAdminDiscountCodeService
{
    private readonly IDiscountCodeRepository _discountCodeRepository = discountCodeRepository;
    private readonly IRestaurantRepository _restaurantRepository = restaurantRepository;

    public async Task<ServiceResult<List<AdminDiscountCodeResponse>>> GetAllAsync(CancellationToken ct = default)
    {
        var codes = await _discountCodeRepository.GetAllUnscopedAsync(ct);
        var result = codes.Select(c => c.ToAdminDto()).ToList();
        return result;
    }

    public async Task<ServiceResult<AdminDiscountCodeResponse>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var code = await _discountCodeRepository.GetByIdWithRestaurantAsync(id, ct);
        if (code is null)
            return ServiceError.NotFound(ErrorMessages.DiscountCodeNotFound);

        return code.ToAdminDto();
    }

    public async Task<ServiceResult<AdminDiscountCodeResponse>> CreateAsync(
        AdminCreateDiscountCodeRequest request, CancellationToken ct = default)
    {
        var restaurant = await _restaurantRepository.GetByIdAsync(request.RestaurantId, ct);
        if (restaurant is null)
            return ServiceError.NotFound(ErrorMessages.RestaurantNotFound);

        if (DiscountValidationHelper.ValidateDateRange(request.ValidFrom, request.ValidUntil, ErrorMessages.InvalidDiscountCodeDates) is { } dateError)
            return dateError;

        if (DiscountValidationHelper.ValidatePercentageDiscount(request.DiscountType, request.DiscountValue) is { } pctError)
            return pctError;

        var existing = await _discountCodeRepository.GetByCodeAndRestaurantAsync(request.Code, request.RestaurantId, ct);
        if (existing is not null)
            return ServiceError.BadRequest(ErrorMessages.DiscountCodeAlreadyExists);

        var discountCode = new DiscountCode
        {
            Code = request.Code,
            Description = request.Description ?? "",
            DiscountType = request.DiscountType,
            DiscountValue = request.DiscountValue,
            MinOrderAmount = request.MinOrderAmount,
            ValidFrom = request.ValidFrom,
            ValidUntil = request.ValidUntil,
            MaxRedemptions = request.MaxRedemptions,
            PerUserLimit = request.PerUserLimit,
            RestaurantId = request.RestaurantId,
            IsActive = request.IsActive
        };

        var created = await _discountCodeRepository.CreateAsync(discountCode, ct);
        created.Restaurant = restaurant;
        created.Redemptions = [];
        return created.ToAdminDto();
    }

    public async Task<ServiceResult<AdminDiscountCodeResponse>> UpdateAsync(
        int id, AdminUpdateDiscountCodeRequest request, CancellationToken ct = default)
    {
        var code = await _discountCodeRepository.GetByIdWithRestaurantAsync(id, ct);
        if (code is null)
            return ServiceError.NotFound(ErrorMessages.DiscountCodeNotFound);

        if (DiscountValidationHelper.ValidateDateRange(request.ValidFrom, request.ValidUntil, ErrorMessages.InvalidDiscountCodeDates) is { } dateError)
            return dateError;

        if (DiscountValidationHelper.ValidatePercentageDiscount(request.DiscountType, request.DiscountValue) is { } pctError)
            return pctError;

        code.Description = request.Description ?? "";
        code.DiscountType = request.DiscountType;
        code.DiscountValue = request.DiscountValue;
        code.MinOrderAmount = request.MinOrderAmount;
        code.ValidFrom = request.ValidFrom;
        code.ValidUntil = request.ValidUntil;
        code.MaxRedemptions = request.MaxRedemptions;
        code.PerUserLimit = request.PerUserLimit;
        code.IsActive = request.IsActive;
        code.UpdatedAt = DateTime.UtcNow;

        var updated = await _discountCodeRepository.UpdateAsync(code, ct);
        return updated.ToAdminDto();
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken ct = default)
    {
        var deleted = await _discountCodeRepository.DeleteAsync(id, ct);
        if (!deleted)
            return ServiceError.NotFound(ErrorMessages.DiscountCodeNotFound);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult<List<AdminRedemptionResponse>>> GetRedemptionsAsync(int discountCodeId, CancellationToken ct = default)
    {
        var code = await _discountCodeRepository.GetByIdAsync(discountCodeId, ct);
        if (code is null)
            return ServiceError.NotFound(ErrorMessages.DiscountCodeNotFound);

        var redemptions = await _discountCodeRepository.GetRedemptionsByCodeIdAsync(discountCodeId, ct);
        var result = redemptions.Select(r => r.ToAdminDto()).ToList();
        return result;
    }
}
