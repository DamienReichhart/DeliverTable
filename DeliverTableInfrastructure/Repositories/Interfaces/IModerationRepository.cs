using DeliverTableInfrastructure.Models;

namespace DeliverTableInfrastructure.Repositories.Interfaces;

/// <summary>
///     Pure data-access abstraction for <see cref="ModerationAction"/> entities.
///     No DTO mapping or business logic -- those belong in the service layer.
/// </summary>
public interface IModerationRepository
{
    Task<List<ModerationAction>> GetAllAsync(CancellationToken ct = default);
    Task<ModerationAction?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ModerationAction> CreateAsync(ModerationAction action, CancellationToken ct = default);
}
