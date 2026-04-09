using DeliverTableInfrastructure.Data;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableSharedLibrary.Enums;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableInfrastructure.Repositories;

public class EmailJobRepository(DeliverTableContext context) : IEmailJobRepository
{
    public async Task<EmailJob?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await context.EmailJobs.FindAsync([id], ct);
    }

    public async Task CreateAsync(EmailJob job, CancellationToken ct = default)
    {
        context.EmailJobs.Add(job);
        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(EmailJob job, CancellationToken ct = default)
    {
        context.EmailJobs.Update(job);
        await context.SaveChangesAsync(ct);
    }

    public async Task<List<EmailJob>> GetStaleJobsByStatusAsync(
        EmailJobStatus status, DateTime olderThan, CancellationToken ct = default)
    {
        return await context.EmailJobs
            .Where(j => j.Status == status && j.CreatedAt < olderThan)
            .ToListAsync(ct);
    }
}
