using DeliverTableServer.Common;
using DeliverTableServer.Extensions;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Invoice;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeliverTableServer.Features.Admin;

[ApiController]
[Route(ApiRoutes.Admin.Base)]
[Authorize(Roles = nameof(UserRole.Administrator))]
public class AdminInvoiceController(IInvoiceService invoiceService) : ControllerBase
{
    private readonly IInvoiceService _invoiceService = invoiceService;

    [HttpGet(ApiRoutes.Admin.InvoicesRoute)]
    public async Task<IActionResult> List([FromQuery] InvoiceAdminQuery query, CancellationToken ct)
    {
        ServiceResult<PaginatedResult<AdminInvoiceRowDto>> result = await _invoiceService.AdminListAsync(query, ct);
        return result.ToOkResult();
    }

    [HttpGet(ApiRoutes.Admin.InvoiceByIdRoute)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        ServiceResult<AdminInvoiceDetailDto> result = await _invoiceService.AdminGetDetailAsync(id, ct);
        return result.ToOkResult();
    }

    [HttpPost(ApiRoutes.Admin.InvoiceByIdRoute + "/resend-email")]
    public async Task<IActionResult> ResendEmail(int id, CancellationToken ct)
    {
        ServiceResult result = await _invoiceService.AdminResendEmailAsync(id, ct);
        return result.ToNoContentResult();
    }
}
