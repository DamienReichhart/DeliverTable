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
public class AdminModerationController(IAdminModerationService adminModerationService) : ControllerBase
{
    private readonly IAdminModerationService _adminModerationService = adminModerationService;

    [HttpGet(ApiRoutes.Admin.ModerationRoute)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _adminModerationService.GetAllAsync(ct);
        return result.ToOkResult();
    }

    [HttpGet(ApiRoutes.Admin.ModerationByIdRoute)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _adminModerationService.GetByIdAsync(id, ct);
        return result.ToOkResult();
    }

    [HttpPost(ApiRoutes.Admin.ModerationRoute)]
    public async Task<IActionResult> Create(
        [FromBody] AdminCreateModerationActionRequest request, CancellationToken ct)
    {
        if (!this.TryGetUserId(out int adminUserId)) return Unauthorized();

        var result = await _adminModerationService.CreateAsync(request, adminUserId, ct);
        if (!result.IsSuccess)
            return result.Error!.ToErrorResult();

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }
}
