using DeliverTableServer.Extensions;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Constants.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeliverTableServer.Controllers;

[ApiController]
[Route(ApiRoutes.Admin.Base)]
[Authorize(Roles = nameof(UserRole.Administrator))]
public class AdminTransactionController(IAdminTransactionService adminTransactionService) : ControllerBase
{
    private readonly IAdminTransactionService _adminTransactionService = adminTransactionService;

    [HttpGet(ApiRoutes.Admin.TransactionsRoute)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _adminTransactionService.GetAllAsync(ct);
        return result.ToOkResult();
    }

    [HttpGet(ApiRoutes.Admin.TransactionByIdRoute)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _adminTransactionService.GetByIdAsync(id, ct);
        return result.ToOkResult();
    }
}
