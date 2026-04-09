using DeliverTableServer.Common;
using DeliverTableServer.Constants;
using DeliverTableServer.Mappers;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Admin;

namespace DeliverTableServer.Services;

public sealed class AdminRatingService(IRatingRepository ratingRepository) : IAdminRatingService
{
    private readonly IRatingRepository _ratingRepository = ratingRepository;

    public async Task<ServiceResult<List<AdminRestaurantRatingResponse>>> GetRestaurantRatingsAsync(
        CancellationToken ct = default)
    {
        var ratings = await _ratingRepository.GetAllRestaurantRatingsAsync(ct);
        var result = ratings.Select(r => r.ToAdminDto()).ToList();
        return result;
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken ct = default)
    {
        var deleted = await _ratingRepository.DeleteRestaurantRatingAsync(id, ct);
        if (deleted)
            return ServiceResult.Success();

        return new ServiceError(ErrorMessages.RatingNotFound, 404);
    }
}
