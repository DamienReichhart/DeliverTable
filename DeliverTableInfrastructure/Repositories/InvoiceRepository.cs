using DeliverTableInfrastructure.Data;
using DeliverTableInfrastructure.Models;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableSharedLibrary.Enums;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableInfrastructure.Repositories;

public class InvoiceRepository(DeliverTableContext dbContext) : IInvoiceRepository
{
    private readonly DeliverTableContext _dbContext = dbContext;

    public async Task<Invoice> CreateAsync(Invoice invoice, CancellationToken ct = default)
    {
        _dbContext.Invoices.Add(invoice);
        await _dbContext.SaveChangesAsync(ct);
        return invoice;
    }

    public async Task CreateBatchAsync(IEnumerable<Invoice> invoices, CancellationToken ct = default)
    {
        _dbContext.Invoices.AddRange(invoices);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Invoice invoice, CancellationToken ct = default)
    {
        invoice.UpdatedAt = DateTime.UtcNow;
        _dbContext.Invoices.Update(invoice);
        await _dbContext.SaveChangesAsync(ct);
    }

    public Task<Invoice?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _dbContext.Invoices.FirstOrDefaultAsync(i => i.Id == id, ct);

    public Task<Invoice?> GetByIdWithLinesAsync(int id, CancellationToken ct = default) =>
        _dbContext.Invoices.Include(i => i.Lines).FirstOrDefaultAsync(i => i.Id == id, ct);

    public Task<Invoice?> GetByIdWithLinesAndRecipientsAsync(int id, CancellationToken ct = default) =>
        _dbContext.Invoices
            .Include(i => i.Lines)
            .Include(i => i.RecipientUser)
            .Include(i => i.RecipientRestaurant).ThenInclude(r => r!.Owner)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

    public Task<bool> ExistsForOrderAndKindAsync(int orderId, InvoiceKind kind, CancellationToken ct = default) =>
        _dbContext.Invoices.AnyAsync(i => i.OrderId == orderId && i.Kind == kind, ct);

    public Task<List<Invoice>> ListByOrderIdAsync(int orderId, CancellationToken ct = default) =>
        _dbContext.Invoices.Where(i => i.OrderId == orderId).ToListAsync(ct);

    public Task<List<Invoice>> ListOriginalsByOrderIdAsync(int orderId, CancellationToken ct = default) =>
        _dbContext.Invoices
            .Where(i => i.OrderId == orderId
                     && (i.Kind == InvoiceKind.OrderInvoiceToCustomer
                         || i.Kind == InvoiceKind.CommissionInvoiceToRestaurant))
            .ToListAsync(ct);

    public async Task<(List<Invoice> Items, int Total)> ListForRecipientUserAsync(int userId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _dbContext.Invoices.Where(i => i.RecipientUserId == userId);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(i => i.IssuedAt)
                               .Skip((page - 1) * pageSize).Take(pageSize)
                               .ToListAsync(ct);
        return (items, total);
    }

    public async Task<(List<Invoice> Items, int Total)> ListForRecipientRestaurantAsync(int restaurantId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _dbContext.Invoices.Where(i => i.RecipientRestaurantId == restaurantId);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(i => i.IssuedAt)
                               .Skip((page - 1) * pageSize).Take(pageSize)
                               .ToListAsync(ct);
        return (items, total);
    }

    public async Task<(List<Invoice> Items, int Total)> AdminListAsync(int? year, InvoiceKind? kind, InvoiceIssuerType? issuerType, int? restaurantId, string? customerEmailContains, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _dbContext.Invoices.Include(i => i.RecipientUser).AsQueryable();
        if (year.HasValue) query = query.Where(i => i.IssuedAt.Year == year.Value);
        if (kind.HasValue) query = query.Where(i => i.Kind == kind.Value);
        if (issuerType.HasValue) query = query.Where(i => i.IssuerType == issuerType.Value);
        if (restaurantId.HasValue) query = query.Where(i => i.IssuerRestaurantId == restaurantId.Value || i.RecipientRestaurantId == restaurantId.Value);
        if (!string.IsNullOrEmpty(customerEmailContains))
            query = query.Where(i => i.RecipientUser != null && i.RecipientUser.Email != null && i.RecipientUser.Email.Contains(customerEmailContains));

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(i => i.IssuedAt)
                               .Skip((page - 1) * pageSize).Take(pageSize)
                               .ToListAsync(ct);
        return (items, total);
    }
}
