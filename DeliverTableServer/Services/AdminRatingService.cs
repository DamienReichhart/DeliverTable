using DeliverTableServer.Common;
using DeliverTableServer.Constants;
using DeliverTableServer.Mappers;
using DeliverTableServer.Repositories.Interfaces;
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

    public async Task<ServiceResult<List<AdminCustomerRatingResponse>>> GetCustomerRatingsAsync(
        CancellationToken ct = default)
    {
        var ratings = await _ratingRepository.GetAllCustomerRatingsAsync(ct);
        var result = ratings.Select(r => r.ToAdminDto()).ToList();
        return result;
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken ct = default)
    {
        var deletedRestaurant = await _ratingRepository.DeleteRestaurantRatingAsync(id, ct);
        if (deletedRestaurant)
            return ServiceResult.Success();

        var deletedCustomer = await _ratingRepository.DeleteCustomerRatingAsync(id, ct);
        if (deletedCustomer)
            return ServiceResult.Success();

        return new ServiceError(ErrorMessages.RatingNotFound, 404);
    }
}
