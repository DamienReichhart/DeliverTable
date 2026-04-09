using DeliverTableInfrastructure.Data;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableInfrastructure.Repositories;

public class RatingRepository(DeliverTableContext dbContext) : IRatingRepository
{
    private readonly DeliverTableContext _dbContext = dbContext;

    public async Task<List<RestaurantRating>> GetAllRestaurantRatingsAsync(CancellationToken ct = default)
    {
        return await _dbContext.RestaurantRatings
            .Include(r => r.Restaurant)
            .Include(r => r.CustomerUser)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<bool> DeleteRestaurantRatingAsync(int id, CancellationToken ct = default)
    {
        var rating = await _dbContext.RestaurantRatings.FindAsync([id], ct);
        if (rating is null) return false;
        _dbContext.RestaurantRatings.Remove(rating);
        await _dbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task<RestaurantRating?> GetByOrderAndCustomerAsync(int orderId, int customerId, CancellationToken ct = default)
    {
        return await _dbContext.RestaurantRatings
            .Include(r => r.Restaurant)
            .FirstOrDefaultAsync(r => r.OrderId == orderId && r.CustomerUserId == customerId, ct);
    }

    public async Task<RestaurantRating> CreateAsync(RestaurantRating rating, CancellationToken ct = default)
    {
        _dbContext.RestaurantRatings.Add(rating);
        await _dbContext.SaveChangesAsync(ct);
        return rating;
    }

    public async Task<RestaurantRating> UpdateAsync(RestaurantRating rating, CancellationToken ct = default)
    {
        _dbContext.RestaurantRatings.Update(rating);
        await _dbContext.SaveChangesAsync(ct);
        return rating;
    }

    public async Task DeleteAsync(RestaurantRating rating, CancellationToken ct = default)
    {
        _dbContext.RestaurantRatings.Remove(rating);
        await _dbContext.SaveChangesAsync(ct);
    }
}
