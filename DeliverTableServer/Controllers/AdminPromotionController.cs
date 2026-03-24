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
public class AdminPromotionController(IAdminPromotionService adminPromotionService) : ControllerBase
{
    private readonly IAdminPromotionService _adminPromotionService = adminPromotionService;

    [HttpGet(ApiRoutes.Admin.PromotionsRoute)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _adminPromotionService.GetAllAsync(ct);
        return result.ToOkResult();
    }

    [HttpGet(ApiRoutes.Admin.PromotionByIdRoute)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _adminPromotionService.GetByIdAsync(id, ct);
        return result.ToOkResult();
    }

    [HttpPost(ApiRoutes.Admin.PromotionsRoute)]
    public async Task<IActionResult> Create([FromBody] AdminCreatePromotionRequest request, CancellationToken ct)
    {
        var result = await _adminPromotionService.CreateAsync(request, ct);
        if (!result.IsSuccess)
            return result.Error!.ToErrorResult();

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut(ApiRoutes.Admin.PromotionByIdRoute)]
    public async Task<IActionResult> Update(int id, [FromBody] AdminUpdatePromotionRequest request, CancellationToken ct)
    {
        var result = await _adminPromotionService.UpdateAsync(id, request, ct);
        return result.ToOkResult();
    }

    [HttpDelete(ApiRoutes.Admin.PromotionByIdRoute)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _adminPromotionService.DeleteAsync(id, ct);
        return result.ToNoContentResult();
    }
}
