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
public class AdminDishController(IAdminDishService adminDishService) : ControllerBase
{
    private readonly IAdminDishService _adminDishService = adminDishService;

    [HttpGet(ApiRoutes.Admin.DishesRoute)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _adminDishService.GetAllAsync(ct);
        return result.ToOkResult();
    }

    [HttpGet(ApiRoutes.Admin.DishByIdRoute)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _adminDishService.GetByIdAsync(id, ct);
        return result.ToOkResult();
    }

    [HttpPost(ApiRoutes.Admin.DishesRoute)]
    public async Task<IActionResult> Create([FromBody] AdminCreateDishRequest request, CancellationToken ct)
    {
        var result = await _adminDishService.CreateAsync(request, ct);
        if (!result.IsSuccess)
            return result.Error!.ToErrorResult();

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut(ApiRoutes.Admin.DishByIdRoute)]
    public async Task<IActionResult> Update(int id, [FromBody] AdminUpdateDishRequest request, CancellationToken ct)
    {
        var result = await _adminDishService.UpdateAsync(id, request, ct);
        return result.ToOkResult();
    }

    [HttpDelete(ApiRoutes.Admin.DishByIdRoute)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _adminDishService.DeleteAsync(id, ct);
        return result.ToNoContentResult();
    }
}
