using DeliverTableServer.Models;

namespace DeliverTableServer.Repositories.Interfaces;

/// <summary>
///     Pure data-access abstraction for <see cref="Event"/> entities.
///     No DTO mapping or business logic -- those belong in the service layer.
/// </summary>
public interface IEventRepository
{
    Task<List<Event>> GetAllAsync(CancellationToken ct = default);
    Task<Event?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Event> CreateAsync(Event evt, CancellationToken ct = default);
    Task<Event> UpdateAsync(Event evt, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
