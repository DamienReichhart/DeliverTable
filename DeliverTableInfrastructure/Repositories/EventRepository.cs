using DeliverTableInfrastructure.Data;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableInfrastructure.Repositories;

public class EventRepository(DeliverTableContext dbContext) : IEventRepository
{
    private readonly DeliverTableContext _dbContext = dbContext;

    public async Task<List<Event>> GetAllAsync(CancellationToken ct = default)
        => await _dbContext.Events
            .Include(e => e.Restaurant)
            .Include(e => e.CreatedByUser)
            .OrderBy(e => e.Id)
            .ToListAsync(ct);

    public async Task<Event?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _dbContext.Events
            .Include(e => e.Restaurant)
            .Include(e => e.CreatedByUser)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<Event> CreateAsync(Event evt, CancellationToken ct = default)
    {
        _dbContext.Events.Add(evt);
        await _dbContext.SaveChangesAsync(ct);
        return evt;
    }

    public async Task<Event> UpdateAsync(Event evt, CancellationToken ct = default)
    {
        _dbContext.Events.Update(evt);
        await _dbContext.SaveChangesAsync(ct);
        return evt;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        Event? evt = await _dbContext.Events.FindAsync([id], ct);
        if (evt is null) return false;

        _dbContext.Events.Remove(evt);
        await _dbContext.SaveChangesAsync(ct);
        return true;
    }
}
