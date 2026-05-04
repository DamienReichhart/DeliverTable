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
public class AdminOrderConfigController(IAdminOrderConfigService adminOrderConfigService) : ControllerBase
{
    private readonly IAdminOrderConfigService _adminOrderConfigService = adminOrderConfigService;

    // ── OrderRule ──

    [HttpGet(ApiRoutes.Admin.OrderRulesRoute)]
    public async Task<IActionResult> GetAllRules(CancellationToken ct)
    {
        var result = await _adminOrderConfigService.GetAllRulesAsync(ct);
        return result.ToOkResult();
    }

    [HttpGet(ApiRoutes.Admin.OrderRuleByIdRoute)]
    public async Task<IActionResult> GetRuleById(int id, CancellationToken ct)
    {
        var result = await _adminOrderConfigService.GetRuleByIdAsync(id, ct);
        return result.ToOkResult();
    }

    [HttpPost(ApiRoutes.Admin.OrderRulesRoute)]
    public async Task<IActionResult> CreateRule([FromBody] AdminCreateOrderRuleRequest request, CancellationToken ct)
    {
        var result = await _adminOrderConfigService.CreateRuleAsync(request, ct);
        return result.ToCreatedResult(nameof(GetRuleById), v => new { id = v.Id });
    }

    [HttpPut(ApiRoutes.Admin.OrderRuleByIdRoute)]
    public async Task<IActionResult> UpdateRule(int id, [FromBody] AdminUpdateOrderRuleRequest request,
        CancellationToken ct)
    {
        var result = await _adminOrderConfigService.UpdateRuleAsync(id, request, ct);
        return result.ToOkResult();
    }

    [HttpDelete(ApiRoutes.Admin.OrderRuleByIdRoute)]
    public async Task<IActionResult> DeleteRule(int id, CancellationToken ct)
    {
        var result = await _adminOrderConfigService.DeleteRuleAsync(id, ct);
        return result.ToNoContentResult();
    }

    // ── OrderBlockedSlot ──

    [HttpGet(ApiRoutes.Admin.BlockedSlotsRoute)]
    public async Task<IActionResult> GetAllBlockedSlots(CancellationToken ct)
    {
        var result = await _adminOrderConfigService.GetAllBlockedSlotsAsync(ct);
        return result.ToOkResult();
    }

    [HttpGet(ApiRoutes.Admin.BlockedSlotByIdRoute)]
    public async Task<IActionResult> GetBlockedSlotById(int id, CancellationToken ct)
    {
        var result = await _adminOrderConfigService.GetBlockedSlotByIdAsync(id, ct);
        return result.ToOkResult();
    }

    [HttpPost(ApiRoutes.Admin.BlockedSlotsRoute)]
    public async Task<IActionResult> CreateBlockedSlot([FromBody] AdminCreateBlockedSlotRequest request,
        CancellationToken ct)
    {
        var result = await _adminOrderConfigService.CreateBlockedSlotAsync(request, ct);
        return result.ToCreatedResult(nameof(GetBlockedSlotById), v => new { id = v.Id });
    }

    [HttpDelete(ApiRoutes.Admin.BlockedSlotByIdRoute)]
    public async Task<IActionResult> DeleteBlockedSlot(int id, CancellationToken ct)
    {
        var result = await _adminOrderConfigService.DeleteBlockedSlotAsync(id, ct);
        return result.ToNoContentResult();
    }
}
