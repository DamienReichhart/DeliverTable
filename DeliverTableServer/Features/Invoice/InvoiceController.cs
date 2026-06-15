using DeliverTableServer.Common;
using DeliverTableServer.Extensions;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Invoice;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeliverTableServer.Features.Invoice;

[ApiController]
[Route(ApiRoutes.Invoice.Base)]
[Authorize]
public class InvoiceController(IInvoiceService invoiceService) : ControllerBase
{
    private readonly IInvoiceService _invoiceService = invoiceService;

    [HttpGet(ApiRoutes.Invoice.MyListRoute)]
    [Authorize(Roles = nameof(UserRole.Customer))]
    public async Task<IActionResult> GetMine(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (!this.TryGetUserId(out int userId)) return Unauthorized();
        ServiceResult<PaginatedResult<InvoiceListItemDto>> result = await _invoiceService.ListForMeAsync(userId, page, pageSize, ct);
        return result.ToOkResult();
    }

    [HttpGet(ApiRoutes.Invoice.RestaurantListRoute)]
    [Authorize(Roles = nameof(UserRole.RestaurantOwner) + "," + nameof(UserRole.Administrator))]
    public async Task<IActionResult> GetForRestaurant(
        [FromRoute] int id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (!this.TryGetUserId(out int userId)) return Unauthorized();
        bool isAdmin = User.IsInRole(nameof(UserRole.Administrator));
        ServiceResult<PaginatedResult<InvoiceListItemDto>> result = await _invoiceService.ListForRestaurantAsync(id, userId, isAdmin, page, pageSize, ct);
        return result.ToOkResult();
    }

    [HttpGet(ApiRoutes.Invoice.DownloadRoute)]
    public async Task<IActionResult> Download([FromRoute] int id, CancellationToken ct)
    {
        if (!this.TryGetUserId(out int userId)) return Unauthorized();
        bool isAdmin = User.IsInRole(nameof(UserRole.Administrator));
        bool isRestaurantOwner = User.IsInRole(nameof(UserRole.RestaurantOwner));
        ServiceResult<InvoicePdfStreamResult> result = await _invoiceService.GetPdfStreamAsync(id, userId, isAdmin, isRestaurantOwner, ct);
        if (!result.IsSuccess) return result.Error!.ToErrorResult();
        InvoicePdfStreamResult payload = result.Value!;
        return File(payload.Stream, payload.ContentType, payload.FileName);
    }
}
