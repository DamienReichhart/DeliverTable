using DeliverTableInfrastructure.Data;
using DeliverTableInfrastructure.Extensions;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableSharedLibrary.Dtos.Dish;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableInfrastructure.Repositories;

public class DishRepository(DeliverTableContext dbContext) : IDishRepository
{
    private readonly DeliverTableContext _dbContext = dbContext;

    public async Task<(List<Dish> Items, int TotalCount)> GetAllAsync(DishQuery query, CancellationToken ct = default)
    {
        var q = _dbContext.Dishes.AsQueryable();
        q = ApplyFilters(q, query);
        q = q.OrderBy(d => d.Id);

        var totalCount = await q.CountAsync(ct);
        var items = await q.Paginate(query.PageNumber, query.PageSize).ToListAsync(ct);
        return (items, totalCount);
    }

    public async Task<(List<Dish> Items, int TotalCount)> GetByRestaurantIdAsync(
        DishQuery query, int restaurantId, CancellationToken ct = default)
    {
        var q = _dbContext.Dishes.Where(d => d.RestaurantId == restaurantId);
        q = ApplyFilters(q, query);
        q = q.OrderBy(d => d.Id);

        var totalCount = await q.CountAsync(ct);
        var items = await q.Paginate(query.PageNumber, query.PageSize).ToListAsync(ct);
        return (items, totalCount);
    }

    public async Task<Dish?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _dbContext.Dishes.FindAsync([id], ct);

    public async Task<Dish> CreateAsync(Dish dish, CancellationToken ct = default)
    {
        _dbContext.Dishes.Add(dish);
        await _dbContext.SaveChangesAsync(ct);
        return dish;
    }

    public async Task<Dish> UpdateAsync(Dish dish, CancellationToken ct = default)
    {
        _dbContext.Dishes.Update(dish);
        await _dbContext.SaveChangesAsync(ct);
        return dish;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var dish = await _dbContext.Dishes.FindAsync([id], ct);
        if (dish is null) return false;

        _dbContext.Dishes.Remove(dish);
        await _dbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task<List<Dish>> GetAllUnscopedAsync(CancellationToken ct = default)
        => await _dbContext.Dishes.Include(d => d.Restaurant).OrderBy(d => d.Id).ToListAsync(ct);

    public async Task<Dish?> GetByIdWithRestaurantAsync(int id, CancellationToken ct = default)
        => await _dbContext.Dishes.Include(d => d.Restaurant).FirstOrDefaultAsync(d => d.Id == id, ct);

    private static IQueryable<Dish> ApplyFilters(IQueryable<Dish> query, DishQuery filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Name))
            query = query.Where(d => d.Name.Contains(filter.Name));
        if (filter.LessThanPrice is not null)
            query = query.Where(d => d.BasePrice < filter.LessThanPrice);
        if (filter.IsVegetarian is not null)
            query = query.Where(d => d.IsVegetarian == filter.IsVegetarian);
        if (filter.IsVegan is not null)
            query = query.Where(d => d.IsVegan == filter.IsVegan);
        if (filter.IsGlutenFree is not null)
            query = query.Where(d => d.IsGlutenFree == filter.IsGlutenFree);
        if (filter.IsAllergenHazard is not null)
            query = query.Where(d => d.IsAllergenHazard == filter.IsAllergenHazard);
        if (filter.IsDishOfTheDay is not null)
            query = query.Where(d => d.IsDishOfTheDay == filter.IsDishOfTheDay);
        return query;
    }
}
