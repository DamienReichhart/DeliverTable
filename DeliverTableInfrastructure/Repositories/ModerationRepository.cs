using DeliverTableInfrastructure.Data;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableInfrastructure.Repositories;

public class ModerationRepository(DeliverTableContext dbContext) : IModerationRepository
{
    private readonly DeliverTableContext _dbContext = dbContext;

    public async Task<List<ModerationAction>> GetAllAsync(CancellationToken ct = default)
        => await _dbContext.ModerationActions
            .Include(m => m.AdminUser)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(ct);

    public async Task<ModerationAction?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _dbContext.ModerationActions
            .Include(m => m.AdminUser)
            .FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task<ModerationAction> CreateAsync(ModerationAction action, CancellationToken ct = default)
    {
        _dbContext.ModerationActions.Add(action);
        await _dbContext.SaveChangesAsync(ct);
        return action;
    }
}
