using DeliverTableInfrastructure.Models;

namespace DeliverTableInfrastructure.Repositories.Interfaces;

public interface IRatingRepository
{
    // Admin
    Task<List<RestaurantRating>> GetAllRestaurantRatingsAsync(CancellationToken ct = default);
    Task<bool> DeleteRestaurantRatingAsync(int id, CancellationToken ct = default);

    // Customer-facing
    Task<RestaurantRating?> GetByOrderAndCustomerAsync(int orderId, int customerId, CancellationToken ct = default);
    Task<RestaurantRating> CreateAsync(RestaurantRating rating, CancellationToken ct = default);
    Task<RestaurantRating> UpdateAsync(RestaurantRating rating, CancellationToken ct = default);
    Task DeleteAsync(RestaurantRating rating, CancellationToken ct = default);
}
