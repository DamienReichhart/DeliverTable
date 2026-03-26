using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Rating;

namespace DeliverTableClient.Services.Interfaces;

public interface IRatingClientService
{
    Task<(RatingDto?, ErrorResponse?)> GetByOrderAsync(
        int orderId,
        CancellationToken ct = default
    );

    Task<(RatingDto?, ErrorResponse?)> CreateAsync(
        int orderId,
        CreateRatingRequest request,
        CancellationToken ct = default
    );

    Task<(RatingDto?, ErrorResponse?)> UpdateAsync(
        int orderId,
        UpdateRatingRequest request,
        CancellationToken ct = default
    );

    Task<(bool, ErrorResponse?)> DeleteAsync(int orderId, CancellationToken ct = default);
}
