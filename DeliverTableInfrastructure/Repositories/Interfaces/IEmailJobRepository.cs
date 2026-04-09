using DeliverTableInfrastructure.Models;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableInfrastructure.Repositories.Interfaces;

public interface IEmailJobRepository
{
    Task<EmailJob?> GetByIdAsync(int id, CancellationToken ct = default);
    Task CreateAsync(EmailJob job, CancellationToken ct = default);
    Task UpdateAsync(EmailJob job, CancellationToken ct = default);
    Task<List<EmailJob>> GetStaleJobsByStatusAsync(EmailJobStatus status, DateTime olderThan, CancellationToken ct = default);
}
