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
public class AdminModerationController(IAdminModerationService adminModerationService) : ControllerBase
{
    private readonly IAdminModerationService _adminModerationService = adminModerationService;

    [HttpGet(ApiRoutes.Admin.ModerationRoute)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        ServiceResult<List<AdminModerationActionResponse>> result = await _adminModerationService.GetAllAsync(ct);
        return result.ToOkResult();
    }

    [HttpGet(ApiRoutes.Admin.ModerationByIdRoute)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        ServiceResult<AdminModerationActionResponse> result = await _adminModerationService.GetByIdAsync(id, ct);
        return result.ToOkResult();
    }

    [HttpPost(ApiRoutes.Admin.ModerationRoute)]
    public async Task<IActionResult> Create(
        [FromBody] AdminCreateModerationActionRequest request, CancellationToken ct)
    {
        if (!this.TryGetUserId(out int adminUserId)) return Unauthorized();

        ServiceResult<AdminModerationActionResponse> result = await _adminModerationService.CreateAsync(request, adminUserId, ct);
        return result.ToCreatedResult(nameof(GetById), v => new { id = v.Id });
    }
}
