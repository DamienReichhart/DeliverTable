using DeliverTableInfrastructure.Messaging.Messages;
using DeliverTableServer.Common;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Invoice;

namespace DeliverTableServer.Services.Interfaces;

public sealed record InvoicePdfStreamResult(Stream Stream, string FileName, string ContentType);

public interface IInvoiceService
{
    Task<ServiceResult<List<InvoiceJobMessage>>> CreatePendingInvoicesForCapturedOrderAsync(
        int orderId,
        CancellationToken ct);

    Task<ServiceResult<List<InvoiceJobMessage>>> CreateCreditNotesForRefundAsync(
        int refundId,
        CancellationToken ct);

    Task<ServiceResult<PaginatedResult<InvoiceListItemDto>>> ListForMeAsync(
        int userId,
        int page,
        int pageSize,
        CancellationToken ct);

    Task<ServiceResult<PaginatedResult<InvoiceListItemDto>>> ListForRestaurantAsync(
        int restaurantId,
        int userId,
        bool isAdmin,
        int page,
        int pageSize,
        CancellationToken ct);

    Task<ServiceResult<InvoicePdfStreamResult>> GetPdfStreamAsync(
        int invoiceId,
        int userId,
        bool isAdmin,
        bool isRestaurantOwner,
        CancellationToken ct);

    Task<ServiceResult<PaginatedResult<AdminInvoiceRowDto>>> AdminListAsync(
        InvoiceAdminQuery query,
        CancellationToken ct);

    Task<ServiceResult<AdminInvoiceDetailDto>> AdminGetDetailAsync(int id, CancellationToken ct);

    Task<ServiceResult> AdminResendEmailAsync(int id, CancellationToken ct);
}
