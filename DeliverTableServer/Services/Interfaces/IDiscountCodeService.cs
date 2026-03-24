using DeliverTableServer.Common;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.DiscountCode;

namespace DeliverTableServer.Services.Interfaces;

public interface IDiscountCodeService
{
    Task<ServiceResult<DiscountCodeDto>> CreateAsync(int restaurantId, int ownerId, CreateDiscountCodeRequest request, CancellationToken ct = default);
    Task<ServiceResult<PaginatedResult<DiscountCodeDto>>> GetByRestaurantAsync(int restaurantId, int ownerId, DiscountCodeQuery query, CancellationToken ct = default);
    Task<ServiceResult<DiscountCodeDto>> UpdateAsync(int discountCodeId, int ownerId, UpdateDiscountCodeRequest request, CancellationToken ct = default);
    Task<ServiceResult> DeleteAsync(int discountCodeId, int ownerId, CancellationToken ct = default);
    Task<ServiceResult<DiscountCodeDto>> ValidateAsync(int restaurantId, int customerId, string code, CancellationToken ct = default);
}
