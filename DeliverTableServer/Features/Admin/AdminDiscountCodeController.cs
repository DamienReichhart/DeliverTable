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
public class AdminDiscountCodeController(IAdminDiscountCodeService adminDiscountCodeService) : ControllerBase
{
    private readonly IAdminDiscountCodeService _adminDiscountCodeService = adminDiscountCodeService;

    [HttpGet(ApiRoutes.Admin.DiscountCodesRoute)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        ServiceResult<List<AdminDiscountCodeResponse>> result = await _adminDiscountCodeService.GetAllAsync(ct);
        return result.ToOkResult();
    }

    [HttpGet(ApiRoutes.Admin.DiscountCodeByIdRoute)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        ServiceResult<AdminDiscountCodeResponse> result = await _adminDiscountCodeService.GetByIdAsync(id, ct);
        return result.ToOkResult();
    }

    [HttpPost(ApiRoutes.Admin.DiscountCodesRoute)]
    public async Task<IActionResult> Create([FromBody] AdminCreateDiscountCodeRequest request, CancellationToken ct)
    {
        ServiceResult<AdminDiscountCodeResponse> result = await _adminDiscountCodeService.CreateAsync(request, ct);
        return result.ToCreatedResult(nameof(GetById), v => new { id = v.Id });
    }

    [HttpPut(ApiRoutes.Admin.DiscountCodeByIdRoute)]
    public async Task<IActionResult> Update(int id, [FromBody] AdminUpdateDiscountCodeRequest request, CancellationToken ct)
    {
        ServiceResult<AdminDiscountCodeResponse> result = await _adminDiscountCodeService.UpdateAsync(id, request, ct);
        return result.ToOkResult();
    }

    [HttpDelete(ApiRoutes.Admin.DiscountCodeByIdRoute)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        ServiceResult result = await _adminDiscountCodeService.DeleteAsync(id, ct);
        return result.ToNoContentResult();
    }

    [HttpGet(ApiRoutes.Admin.DiscountCodeRedemptionsRoute)]
    public async Task<IActionResult> GetRedemptions(int id, CancellationToken ct)
    {
        ServiceResult<List<AdminRedemptionResponse>> result = await _adminDiscountCodeService.GetRedemptionsAsync(id, ct);
        return result.ToOkResult();
    }
}
