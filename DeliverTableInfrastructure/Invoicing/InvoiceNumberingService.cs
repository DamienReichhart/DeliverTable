using DeliverTableInfrastructure.Data;
using DeliverTableInfrastructure.Models;
using DeliverTableSharedLibrary.Enums;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableInfrastructure.Invoicing;

public class InvoiceNumberingService(DeliverTableContext dbContext) : IInvoiceNumberingService
{
    private readonly DeliverTableContext _dbContext = dbContext;

    public async Task<string> IssueNumberAsync(
        InvoiceIssuerType issuerType,
        int? issuerEntityId,
        int year,
        bool isCreditNote,
        CancellationToken ct)
    {
        const int maxAttempts = 5;
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await IssueOnceAsync(issuerType, issuerEntityId, year, isCreditNote, ct);
            }
            catch (DbUpdateException) when (attempt < maxAttempts)
            {
                // Re-read counter and retry — another concurrent call won this round.
                _dbContext.ChangeTracker.Clear();
                await Task.Delay(10 * attempt, ct);
            }
        }

        // Final attempt — let any DbUpdateException propagate to the caller.
        return await IssueOnceAsync(issuerType, issuerEntityId, year, isCreditNote, ct);
    }

    private async Task<string> IssueOnceAsync(
        InvoiceIssuerType issuerType,
        int? issuerEntityId,
        int year,
        bool isCreditNote,
        CancellationToken ct)
    {
        InvoiceCounter? counter = await _dbContext.InvoiceCounters
            .FirstOrDefaultAsync(c =>
                c.EntityType == issuerType &&
                c.EntityId == issuerEntityId &&
                c.Year == year, ct);

        if (counter is null)
        {
            counter = new InvoiceCounter
            {
                EntityType = issuerType,
                EntityId = issuerEntityId,
                Year = year,
                NextNumber = 1,
            };
            _dbContext.InvoiceCounters.Add(counter);
            try
            {
                await _dbContext.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                _dbContext.Entry(counter).State = EntityState.Detached;
                counter = await _dbContext.InvoiceCounters.FirstAsync(c =>
                    c.EntityType == issuerType &&
                    c.EntityId == issuerEntityId &&
                    c.Year == year, ct);
            }
        }

        int issued = counter.NextNumber;
        counter.NextNumber++;
        await _dbContext.SaveChangesAsync(ct);

        string prefix = issuerType == InvoiceIssuerType.Platform
            ? "DT"
            : $"R{issuerEntityId:D4}";
        string baseNumber = $"{prefix}-{year}-{issued:D6}";
        return isCreditNote ? $"AV-{baseNumber}" : baseNumber;
    }
}
