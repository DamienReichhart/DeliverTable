using DeliverTableServer.Data;
using DeliverTableServer.Models;
using DeliverTableServer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableServer.Repositories;

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

    public async Task<List<CustomerRating>> GetAllCustomerRatingsAsync(CancellationToken ct = default)
    {
        return await _dbContext.CustomerRatings
            .Include(r => r.Restaurant)
            .Include(r => r.RatedCustomerUser)
            .Include(r => r.RestaurantUser)
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

    public async Task<bool> DeleteCustomerRatingAsync(int id, CancellationToken ct = default)
    {
        var rating = await _dbContext.CustomerRatings.FindAsync([id], ct);
        if (rating is null) return false;
        _dbContext.CustomerRatings.Remove(rating);
        await _dbContext.SaveChangesAsync(ct);
        return true;
    }
}
