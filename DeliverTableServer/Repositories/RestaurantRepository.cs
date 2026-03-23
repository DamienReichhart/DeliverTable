using DeliverTableServer.Data;
using DeliverTableServer.Models;
using DeliverTableServer.Repositories.Interfaces;
using DeliverTableSharedLibrary.Dtos.Restaurant;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableServer.Repositories;

public class RestaurantRepository(DeliverTableContext dbContext) : IRestaurantRepository
{
    private readonly DeliverTableContext _dbContext = dbContext;

    public async Task<Restaurant> CreateAsync(Restaurant restaurant, CancellationToken ct = default)
    {
        _dbContext.Restaurants.Add(restaurant);
        await _dbContext.SaveChangesAsync(ct);
        return restaurant;
    }

    public async Task<(List<Restaurant> Items, int TotalCount)> GetAllAsync(RestaurantQuery query, CancellationToken ct = default)
    {
        var q = _dbContext.Restaurants.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Name))
            q = q.Where(r => r.Name.ToLower().Contains(query.Name.ToLower()));
        if (!string.IsNullOrWhiteSpace(query.City))
            q = q.Where(r => r.City.ToLower().Contains(query.City.ToLower()));
        if (!string.IsNullOrWhiteSpace(query.Type))
            q = q.Where(r => r.Type.ToString().ToLower().Contains(query.Type.ToLower()));

        q = q.OrderBy(r => r.Id);

        var totalCount = await q.CountAsync(ct);

        int page = query.PageNumber > 0 ? query.PageNumber : 1;
        int skip = (page - 1) * query.PageSize;

        var items = await q.Skip(skip).Take(query.PageSize).ToListAsync(ct);
        return (items, totalCount);
    }

    public async Task<Restaurant?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _dbContext.Restaurants.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var restaurant = await _dbContext.Restaurants.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (restaurant is null) return false;

        _dbContext.Restaurants.Remove(restaurant);
        await _dbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task<Restaurant> UpdateAsync(Restaurant restaurant, CancellationToken ct = default)
    {
        _dbContext.Restaurants.Update(restaurant);
        await _dbContext.SaveChangesAsync(ct);
        return restaurant;
    }

    public async Task<(List<Restaurant> Items, int TotalCount)> GetByOwnerAsync(
        int ownerId, RestaurantQuery query, CancellationToken ct = default)
    {
        var q = _dbContext.Restaurants.Where(r => r.OwnerId == ownerId);

        if (!string.IsNullOrWhiteSpace(query.Name))
            q = q.Where(r => r.Name.ToLower().Contains(query.Name.ToLower()));
        if (!string.IsNullOrWhiteSpace(query.City))
            q = q.Where(r => r.City.ToLower().Contains(query.City.ToLower()));

        q = q.OrderBy(r => r.Id);

        var totalCount = await q.CountAsync(ct);

        int page = query.PageNumber > 0 ? query.PageNumber : 1;
        int skip = (page - 1) * query.PageSize;

        var items = await q.Skip(skip).Take(query.PageSize).ToListAsync(ct);
        return (items, totalCount);
    }
}
