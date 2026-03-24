using DeliverTableServer.Data;
using DeliverTableServer.Extensions;
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
        var q = _dbContext.Restaurants.Where(r => r.IsActive);

        q = ApplyFilters(q, query);

        if (!string.IsNullOrWhiteSpace(query.Type))
            q = q.Where(r => r.Type.ToString().ToLower().Contains(query.Type.ToLower()));

        if (query is { Latitude: not null, Longitude: not null, RadiusKm: not null })
        {
            var lat = query.Latitude.Value;
            var lon = query.Longitude.Value;
            var radiusKm = query.RadiusKm.Value;

            // Bounding box pre-filter for performance
            var latDelta = radiusKm / 111.0;
            var lonDelta = radiusKm / (111.0 * Math.Cos(lat * Math.PI / 180.0));

            q = q.Where(r =>
                r.Latitude >= lat - latDelta && r.Latitude <= lat + latDelta &&
                r.Longitude >= lon - lonDelta && r.Longitude <= lon + lonDelta);

            // Fetch candidates and apply Haversine in memory
            var candidates = await q.ToListAsync(ct);
            var filtered = candidates
                .Where(r => HaversineDistanceKm(lat, lon, r.Latitude, r.Longitude) <= radiusKm)
                .OrderBy(r => HaversineDistanceKm(lat, lon, r.Latitude, r.Longitude))
                .ToList();

            var totalCount = filtered.Count;
            var (offset, size) = PaginationExtensions.GetPaginationOffsets(query.PageNumber, query.PageSize);
            var items = filtered.Skip(offset).Take(size).ToList();
            return (items, totalCount);
        }

        q = q.OrderBy(r => r.Id);

        var total = await q.CountAsync(ct);

        var (skip, take) = PaginationExtensions.GetPaginationOffsets(query.PageNumber, query.PageSize);
        var result = await q.Skip(skip).Take(take).ToListAsync(ct);
        return (result, total);
    }

    private static double HaversineDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371.0;
        var dLat = (lat2 - lat1) * Math.PI / 180.0;
        var dLon = (lon2 - lon1) * Math.PI / 180.0;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
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

        q = ApplyFilters(q, query);

        q = q.OrderBy(r => r.Id);

        var totalCount = await q.CountAsync(ct);

        var (skip, take) = PaginationExtensions.GetPaginationOffsets(query.PageNumber, query.PageSize);
        var items = await q.Skip(skip).Take(take).ToListAsync(ct);
        return (items, totalCount);
    }

    public async Task<List<Restaurant>> GetAllUnscopedAsync(CancellationToken ct = default)
        => await _dbContext.Restaurants.Include(r => r.Owner).OrderBy(r => r.Id).ToListAsync(ct);

    public async Task<Restaurant?> GetByIdWithOwnerAsync(int id, CancellationToken ct = default)
        => await _dbContext.Restaurants.Include(r => r.Owner).FirstOrDefaultAsync(r => r.Id == id, ct);

    private static IQueryable<Restaurant> ApplyFilters(IQueryable<Restaurant> query, RestaurantQuery filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Name))
            query = query.Where(r => r.Name.ToLower().Contains(filter.Name.ToLower()));
        if (!string.IsNullOrWhiteSpace(filter.City))
            query = query.Where(r => r.City.ToLower().Contains(filter.City.ToLower()));

        return query;
    }
}
