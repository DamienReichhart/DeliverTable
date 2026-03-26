using DeliverTableServer.Models;

namespace DeliverTableServer.Repositories.Interfaces;

public interface IRatingRepository
{
    Task<List<RestaurantRating>> GetAllRestaurantRatingsAsync(CancellationToken ct = default);
    Task<bool> DeleteRestaurantRatingAsync(int id, CancellationToken ct = default);
}
