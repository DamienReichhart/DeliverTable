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
public class AdminRestaurantController(IAdminRestaurantService adminRestaurantService) : ControllerBase
{
    private readonly IAdminRestaurantService _adminRestaurantService = adminRestaurantService;

    [HttpGet(ApiRoutes.Admin.RestaurantsRoute)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        ServiceResult<List<AdminRestaurantResponse>> result = await _adminRestaurantService.GetAllAsync(ct);
        return result.ToOkResult();
    }

    [HttpGet(ApiRoutes.Admin.RestaurantByIdRoute)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        ServiceResult<AdminRestaurantResponse> result = await _adminRestaurantService.GetByIdAsync(id, ct);
        return result.ToOkResult();
    }

    [HttpPut(ApiRoutes.Admin.RestaurantByIdRoute)]
    public async Task<IActionResult> Update(int id, [FromBody] AdminUpdateRestaurantRequest request, CancellationToken ct)
    {
        ServiceResult<AdminRestaurantResponse> result = await _adminRestaurantService.UpdateAsync(id, request, ct);
        return result.ToOkResult();
    }

    [HttpDelete(ApiRoutes.Admin.RestaurantByIdRoute)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        ServiceResult result = await _adminRestaurantService.DeleteAsync(id, ct);
        return result.ToNoContentResult();
    }
}
