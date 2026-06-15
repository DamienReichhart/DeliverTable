using DeliverTableInfrastructure.Data;
using DeliverTableInfrastructure.Extensions;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableSharedLibrary.Dtos.Restaurant;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableInfrastructure.Repositories;

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
        IQueryable<Restaurant> q = _dbContext.Restaurants.Where(r => r.IsActive);

        q = ApplyFilters(q, query);

        if (!string.IsNullOrWhiteSpace(query.Type))
            q = q.Where(r => r.Type.ToString().ToLower().Contains(query.Type.ToLower()));

        if (query is { Latitude: not null, Longitude: not null, RadiusKm: not null })
        {
            double lat = query.Latitude.Value;
            double lon = query.Longitude.Value;
            double radiusKm = query.RadiusKm.Value;

            // Bounding box pre-filter for performance
            double latDelta = radiusKm / 111.0;
            double lonDelta = radiusKm / (111.0 * Math.Cos(lat * Math.PI / 180.0));

            q = q.Where(r =>
                r.Latitude >= lat - latDelta && r.Latitude <= lat + latDelta &&
                r.Longitude >= lon - lonDelta && r.Longitude <= lon + lonDelta);

            // Fetch candidates and apply Haversine in memory
            List<Restaurant> candidates = await q.ToListAsync(ct);
            List<Restaurant> filtered = candidates
                .Where(r => HaversineDistanceKm(lat, lon, r.Latitude, r.Longitude) <= radiusKm)
                .OrderBy(r => HaversineDistanceKm(lat, lon, r.Latitude, r.Longitude))
                .ToList();

            int totalCount = filtered.Count;
            List<Restaurant> items = filtered.Paginate(query.PageNumber, query.PageSize).ToList();
            return (items, totalCount);
        }

        q = q.OrderBy(r => r.Id);

        int total = await q.CountAsync(ct);
        List<Restaurant> result = await q.Paginate(query.PageNumber, query.PageSize).ToListAsync(ct);
        return (result, total);
    }

    private static double HaversineDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371.0;
        double dLat = (lat2 - lat1) * Math.PI / 180.0;
        double dLon = (lon2 - lon1) * Math.PI / 180.0;
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    public async Task<List<Restaurant>> GetForMapAsync(RestaurantQuery query, CancellationToken ct = default)
    {
        IQueryable<Restaurant> q = _dbContext.Restaurants.Where(r => r.IsActive);

        q = ApplyFilters(q, query);

        if (!string.IsNullOrWhiteSpace(query.Type))
            q = q.Where(r => r.Type.ToString().ToLower().Contains(query.Type.ToLower()));

        if (query is not { Latitude: not null, Longitude: not null, RadiusKm: not null })
            return [];

        double lat = query.Latitude.Value;
        double lon = query.Longitude.Value;
        double radiusKm = query.RadiusKm.Value;

        double latDelta = radiusKm / 111.0;
        double lonDelta = radiusKm / (111.0 * Math.Cos(lat * Math.PI / 180.0));

        q = q.Where(r =>
            r.Latitude >= lat - latDelta && r.Latitude <= lat + latDelta &&
            r.Longitude >= lon - lonDelta && r.Longitude <= lon + lonDelta);

        List<Restaurant> candidates = await q.ToListAsync(ct);
        return candidates
            .Where(r => HaversineDistanceKm(lat, lon, r.Latitude, r.Longitude) <= radiusKm)
            .OrderBy(r => HaversineDistanceKm(lat, lon, r.Latitude, r.Longitude))
            .ToList();
    }

    public async Task<Restaurant?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _dbContext.Restaurants.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        // Soft delete: restaurants are referenced by orders, transactions, invoices, etc.
        // (FK_Orders_Restaurants_RestaurantId uses DeleteBehavior.Restrict), so a hard
        // delete would violate those constraints and destroy financial history. Flipping
        // IsActive hides the restaurant from listing queries while preserving its records.
        Restaurant? restaurant = await _dbContext.Restaurants.FirstOrDefaultAsync(r => r.Id == id && r.IsActive, ct);
        if (restaurant is null) return false;

        restaurant.IsActive = false;
        restaurant.UpdatedAt = DateTime.UtcNow;
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
        IQueryable<Restaurant> q = _dbContext.Restaurants.Where(r => r.OwnerId == ownerId);

        q = ApplyFilters(q, query);

        q = q.OrderBy(r => r.Id);

        int totalCount = await q.CountAsync(ct);
        List<Restaurant> items = await q.Paginate(query.PageNumber, query.PageSize).ToListAsync(ct);
        return (items, totalCount);
    }

    public async Task<List<Restaurant>> GetAllUnscopedAsync(CancellationToken ct = default)
        => await _dbContext.Restaurants.Include(r => r.Owner).OrderBy(r => r.Id).ToListAsync(ct);

    public async Task<Restaurant?> GetByIdWithOwnerAsync(int id, CancellationToken ct = default)
        => await _dbContext.Restaurants.Include(r => r.Owner).FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<int> CountActiveTablesByMaxCapacityAsync(
        int restaurantId,
        int maxCapacity,
        CancellationToken ct = default)
    {
        return await _dbContext.RestaurantTables.CountAsync(t =>
            t.RestaurantId == restaurantId
            && t.IsActive
            && t.Capacity <= maxCapacity,
            ct);
    }

    private static IQueryable<Restaurant> ApplyFilters(IQueryable<Restaurant> query, RestaurantQuery filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Name))
            query = query.Where(r => r.Name.ToLower().Contains(filter.Name.ToLower()));
        if (!string.IsNullOrWhiteSpace(filter.City))
            query = query.Where(r => r.City.ToLower().Contains(filter.City.ToLower()));
        query = query.Where(r => r.IsActive == filter.IsActive);

        return query;
    }
}
