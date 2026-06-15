using DeliverTableInfrastructure.Data;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableInfrastructure.Repositories;

public class NotificationRepository(DeliverTableContext dbContext) : INotificationRepository
{
    private readonly DeliverTableContext _dbContext = dbContext;

    public async Task<List<Notification>> GetAllAsync(CancellationToken ct = default)
    {
        return await _dbContext.Notifications
            .Include(n => n.User)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        Notification? notification = await _dbContext.Notifications.FindAsync([id], ct);
        if (notification is null) return false;
        _dbContext.Notifications.Remove(notification);
        await _dbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task CreateAsync(Notification notification, CancellationToken ct = default)
    {
        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task CreateManyAsync(IEnumerable<Notification> notifications, CancellationToken ct = default)
    {
        await _dbContext.Notifications.AddRangeAsync(notifications, ct);
        await _dbContext.SaveChangesAsync(ct);
    }
}
