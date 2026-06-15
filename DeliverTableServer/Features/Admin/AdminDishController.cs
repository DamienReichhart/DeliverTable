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
public class AdminDishController(IAdminDishService adminDishService) : ControllerBase
{
    private readonly IAdminDishService _adminDishService = adminDishService;

    [HttpGet(ApiRoutes.Admin.DishesRoute)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        ServiceResult<List<AdminDishResponse>> result = await _adminDishService.GetAllAsync(ct);
        return result.ToOkResult();
    }

    [HttpGet(ApiRoutes.Admin.DishByIdRoute)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        ServiceResult<AdminDishResponse> result = await _adminDishService.GetByIdAsync(id, ct);
        return result.ToOkResult();
    }

    [HttpPost(ApiRoutes.Admin.DishesRoute)]
    public async Task<IActionResult> Create([FromBody] AdminCreateDishRequest request, CancellationToken ct)
    {
        ServiceResult<AdminDishResponse> result = await _adminDishService.CreateAsync(request, ct);
        return result.ToCreatedResult(nameof(GetById), v => new { id = v.Id });
    }

    [HttpPut(ApiRoutes.Admin.DishByIdRoute)]
    public async Task<IActionResult> Update(int id, [FromBody] AdminUpdateDishRequest request, CancellationToken ct)
    {
        ServiceResult<AdminDishResponse> result = await _adminDishService.UpdateAsync(id, request, ct);
        return result.ToOkResult();
    }

    [HttpDelete(ApiRoutes.Admin.DishByIdRoute)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        ServiceResult result = await _adminDishService.DeleteAsync(id, ct);
        return result.ToNoContentResult();
    }
}
