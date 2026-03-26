using DeliverTableServer.Common;
using DeliverTableSharedLibrary.Dtos.Rating;

namespace DeliverTableServer.Services.Interfaces;

public interface IRatingService
{
    Task<ServiceResult<RatingDto>> CreateAsync(int orderId, int customerId, CreateRatingRequest request, CancellationToken ct = default);
    Task<ServiceResult<RatingDto>> GetByOrderAsync(int orderId, int customerId, CancellationToken ct = default);
    Task<ServiceResult<RatingDto>> UpdateAsync(int orderId, int customerId, UpdateRatingRequest request, CancellationToken ct = default);
    Task<ServiceResult> DeleteAsync(int orderId, int customerId, CancellationToken ct = default);
}
