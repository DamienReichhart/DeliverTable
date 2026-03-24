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
public class AdminDiscountCodeController(IAdminDiscountCodeService adminDiscountCodeService) : ControllerBase
{
    private readonly IAdminDiscountCodeService _adminDiscountCodeService = adminDiscountCodeService;

    [HttpGet(ApiRoutes.Admin.DiscountCodesRoute)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _adminDiscountCodeService.GetAllAsync(ct);
        return result.ToOkResult();
    }

    [HttpGet(ApiRoutes.Admin.DiscountCodeByIdRoute)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _adminDiscountCodeService.GetByIdAsync(id, ct);
        return result.ToOkResult();
    }

    [HttpPost(ApiRoutes.Admin.DiscountCodesRoute)]
    public async Task<IActionResult> Create([FromBody] AdminCreateDiscountCodeRequest request, CancellationToken ct)
    {
        var result = await _adminDiscountCodeService.CreateAsync(request, ct);
        if (!result.IsSuccess)
            return result.Error!.ToErrorResult();

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut(ApiRoutes.Admin.DiscountCodeByIdRoute)]
    public async Task<IActionResult> Update(int id, [FromBody] AdminUpdateDiscountCodeRequest request, CancellationToken ct)
    {
        var result = await _adminDiscountCodeService.UpdateAsync(id, request, ct);
        return result.ToOkResult();
    }

    [HttpDelete(ApiRoutes.Admin.DiscountCodeByIdRoute)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _adminDiscountCodeService.DeleteAsync(id, ct);
        return result.ToNoContentResult();
    }

    [HttpGet(ApiRoutes.Admin.DiscountCodeRedemptionsRoute)]
    public async Task<IActionResult> GetRedemptions(int id, CancellationToken ct)
    {
        var result = await _adminDiscountCodeService.GetRedemptionsAsync(id, ct);
        return result.ToOkResult();
    }
}
