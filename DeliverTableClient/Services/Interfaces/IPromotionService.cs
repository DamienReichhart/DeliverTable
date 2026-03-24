using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Promotion;

namespace DeliverTableClient.Services.Interfaces;

public interface IPromotionService
{
    Task<(PaginatedResult<PromotionDto>?, ErrorResponse?)> GetByRestaurantAsync(int restaurantId, PromotionQuery query, CancellationToken ct = default);
    Task<(PromotionDto?, ErrorResponse?)> CreateAsync(int restaurantId, CreatePromotionRequest request, CancellationToken ct = default);
    Task<(PromotionDto?, ErrorResponse?)> UpdateAsync(int promotionId, UpdatePromotionRequest request, CancellationToken ct = default);
    Task<(bool, ErrorResponse?)> DeleteAsync(int promotionId, CancellationToken ct = default);
    Task<(List<PromotionDto>?, ErrorResponse?)> GetActiveByRestaurantAsync(int restaurantId, CancellationToken ct = default);
}
