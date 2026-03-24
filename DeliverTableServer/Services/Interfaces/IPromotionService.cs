using DeliverTableServer.Common;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Promotion;

namespace DeliverTableServer.Services.Interfaces;

public interface IPromotionService
{
    Task<ServiceResult<PromotionDto>> CreateAsync(int restaurantId, int ownerId, CreatePromotionRequest request, CancellationToken ct = default);
    Task<ServiceResult<PaginatedResult<PromotionDto>>> GetByRestaurantAsync(int restaurantId, int ownerId, PromotionQuery query, CancellationToken ct = default);
    Task<ServiceResult<PromotionDto>> UpdateAsync(int promotionId, int ownerId, UpdatePromotionRequest request, CancellationToken ct = default);
    Task<ServiceResult> DeleteAsync(int promotionId, int ownerId, CancellationToken ct = default);
}
