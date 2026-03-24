using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.DiscountCode;

namespace DeliverTableClient.Services.Interfaces;

public interface IDiscountCodeClientService
{
    Task<(PaginatedResult<DiscountCodeDto>?, ErrorResponse?)> GetByRestaurantAsync(int restaurantId, DiscountCodeQuery query, CancellationToken ct = default);
    Task<(DiscountCodeDto?, ErrorResponse?)> CreateAsync(int restaurantId, CreateDiscountCodeRequest request, CancellationToken ct = default);
    Task<(DiscountCodeDto?, ErrorResponse?)> UpdateAsync(int discountCodeId, UpdateDiscountCodeRequest request, CancellationToken ct = default);
    Task<(bool, ErrorResponse?)> DeleteAsync(int discountCodeId, CancellationToken ct = default);
}
