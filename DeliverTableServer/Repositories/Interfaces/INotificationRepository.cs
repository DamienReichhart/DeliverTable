using DeliverTableServer.Models;

namespace DeliverTableServer.Repositories.Interfaces;

public interface INotificationRepository
{
    Task<List<Notification>> GetAllAsync(CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
