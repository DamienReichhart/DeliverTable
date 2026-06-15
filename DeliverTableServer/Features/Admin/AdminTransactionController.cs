using DeliverTableServer.Common;
using DeliverTableServer.Extensions;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeliverTableServer.Features.Admin;

[ApiController]
[Route(ApiRoutes.Admin.Base)]
[Authorize(Roles = nameof(UserRole.Administrator))]
public class AdminTransactionController(IAdminTransactionService adminTransactionService) : ControllerBase
{
    private readonly IAdminTransactionService _adminTransactionService = adminTransactionService;

    [HttpGet(ApiRoutes.Admin.TransactionsRoute)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        ServiceResult<List<AdminTransactionResponse>> result = await _adminTransactionService.GetAllAsync(ct);
        return result.ToOkResult();
    }

    [HttpGet(ApiRoutes.Admin.TransactionByIdRoute)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        ServiceResult<AdminTransactionResponse> result = await _adminTransactionService.GetByIdAsync(id, ct);
        return result.ToOkResult();
    }
}
