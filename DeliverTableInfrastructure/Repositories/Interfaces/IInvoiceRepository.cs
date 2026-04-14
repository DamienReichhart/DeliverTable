using DeliverTableInfrastructure.Models;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableInfrastructure.Repositories.Interfaces;

public interface IInvoiceRepository
{
    Task<Invoice> CreateAsync(Invoice invoice, CancellationToken ct = default);
    Task UpdateAsync(Invoice invoice, CancellationToken ct = default);
    Task<Invoice?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Invoice?> GetByIdWithLinesAsync(int id, CancellationToken ct = default);
    Task<Invoice?> GetByIdWithLinesAndRecipientsAsync(int id, CancellationToken ct = default);
    Task<bool> ExistsForOrderAndKindAsync(int orderId, InvoiceKind kind, CancellationToken ct = default);
    Task<List<Invoice>> ListByOrderIdAsync(int orderId, CancellationToken ct = default);
    Task<(List<Invoice> Items, int Total)> ListForRecipientUserAsync(int userId, int page, int pageSize, CancellationToken ct = default);
    Task<(List<Invoice> Items, int Total)> ListForRecipientRestaurantAsync(int restaurantId, int page, int pageSize, CancellationToken ct = default);
    Task<(List<Invoice> Items, int Total)> AdminListAsync(int? year, InvoiceKind? kind, InvoiceIssuerType? issuerType, int? restaurantId, string? customerEmailContains, int page, int pageSize, CancellationToken ct = default);
}
