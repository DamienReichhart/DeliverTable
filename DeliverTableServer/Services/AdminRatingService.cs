using DeliverTableServer.Common;
using DeliverTableServer.Constants;
using DeliverTableServer.Mappers;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Dtos.Admin;
using DeliverTableInfrastructure.Models;

namespace DeliverTableServer.Services;

public sealed class AdminRatingService(IRatingRepository ratingRepository) : IAdminRatingService
{
    private readonly IRatingRepository _ratingRepository = ratingRepository;

    public async Task<ServiceResult<List<AdminRestaurantRatingResponse>>> GetRestaurantRatingsAsync(
        CancellationToken ct = default)
    {
        List<RestaurantRating> ratings = await _ratingRepository.GetAllRestaurantRatingsAsync(ct);
        List<AdminRestaurantRatingResponse> result = ratings.Select(r => r.ToAdminDto()).ToList();
        return result;
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken ct = default)
    {
        bool deleted = await _ratingRepository.DeleteRestaurantRatingAsync(id, ct);
        if (deleted)
            return ServiceResult.Success();

        return ServiceError.NotFound(ErrorMessages.RatingNotFound);
    }
}
