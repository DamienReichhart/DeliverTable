using DeliverTableServer.Extensions;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeliverTableServer.Controllers;

[ApiController]
[Route(ApiRoutes.Admin.Base)]
[Authorize(Roles = nameof(UserRole.Administrator))]
public class AdminLoyaltyController(IAdminLoyaltyService adminLoyaltyService) : ControllerBase
{
    private readonly IAdminLoyaltyService _adminLoyaltyService = adminLoyaltyService;

    [HttpGet(ApiRoutes.Admin.LoyaltyRoute)]
    public async Task<IActionResult> GetAllPrograms(CancellationToken ct)
    {
        var result = await _adminLoyaltyService.GetAllProgramsAsync(ct);
        return result.ToOkResult();
    }

    [HttpGet(ApiRoutes.Admin.LoyaltyByIdRoute)]
    public async Task<IActionResult> GetProgramById(int id, CancellationToken ct)
    {
        var result = await _adminLoyaltyService.GetProgramByIdAsync(id, ct);
        return result.ToOkResult();
    }

    [HttpPost(ApiRoutes.Admin.LoyaltyRoute)]
    public async Task<IActionResult> CreateProgram([FromBody] AdminCreateLoyaltyProgramRequest request, CancellationToken ct)
    {
        var result = await _adminLoyaltyService.CreateProgramAsync(request, ct);
        return result.ToCreatedResult(nameof(GetProgramById), v => new { id = v.Id });
    }

    [HttpPut(ApiRoutes.Admin.LoyaltyByIdRoute)]
    public async Task<IActionResult> UpdateProgram(int id, [FromBody] AdminUpdateLoyaltyProgramRequest request, CancellationToken ct)
    {
        var result = await _adminLoyaltyService.UpdateProgramAsync(id, request, ct);
        return result.ToOkResult();
    }

    [HttpDelete(ApiRoutes.Admin.LoyaltyByIdRoute)]
    public async Task<IActionResult> DeleteProgram(int id, CancellationToken ct)
    {
        var result = await _adminLoyaltyService.DeleteProgramAsync(id, ct);
        return result.ToNoContentResult();
    }

    [HttpGet(ApiRoutes.Admin.LoyaltyAccountsRoute)]
    public async Task<IActionResult> GetAccounts(int id, CancellationToken ct)
    {
        var result = await _adminLoyaltyService.GetAccountsAsync(id, ct);
        return result.ToOkResult();
    }

    [HttpGet(ApiRoutes.Admin.LoyaltyTransactionsRoute)]
    public async Task<IActionResult> GetTransactions(int id, int accountId, CancellationToken ct)
    {
        var result = await _adminLoyaltyService.GetTransactionsAsync(accountId, ct);
        return result.ToOkResult();
    }
}
