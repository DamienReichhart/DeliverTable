using DeliverTableInfrastructure.Models;

namespace DeliverTableInfrastructure.Repositories.Interfaces;

public interface INotificationRepository
{
    Task<List<Notification>> GetAllAsync(CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    Task CreateAsync(Notification notification, CancellationToken ct = default);
    Task CreateManyAsync(IEnumerable<Notification> notifications, CancellationToken ct = default);
}
