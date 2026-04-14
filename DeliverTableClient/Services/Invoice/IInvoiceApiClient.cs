using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Invoice;
using DeliverTableSharedLibrary.Enums;

namespace DeliverTableClient.Services.Invoice;

public interface IInvoiceApiClient
{
    Task<PaginatedResult<InvoiceListItemDto>?> GetMineAsync(int page, int pageSize);
    Task<PaginatedResult<InvoiceListItemDto>?> GetForRestaurantAsync(int restaurantId, int page, int pageSize);
    Task<PaginatedResult<AdminInvoiceRowDto>?> AdminListAsync(int? year, InvoiceKind? kind, InvoiceIssuerType? issuerType, int? restaurantId, string? customerEmail, int page, int pageSize);
    Task<AdminInvoiceDetailDto?> AdminGetAsync(int id);
    Task AdminResendEmailAsync(int id);
}
