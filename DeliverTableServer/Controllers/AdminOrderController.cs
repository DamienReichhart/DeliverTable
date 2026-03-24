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
public class AdminOrderController(IAdminOrderService adminOrderService) : ControllerBase
{
    private readonly IAdminOrderService _adminOrderService = adminOrderService;

    [HttpGet(ApiRoutes.Admin.OrdersRoute)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _adminOrderService.GetAllAsync(ct);
        return result.ToOkResult();
    }

    [HttpGet(ApiRoutes.Admin.OrderByIdRoute)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _adminOrderService.GetByIdAsync(id, ct);
        return result.ToOkResult();
    }

    [HttpPut(ApiRoutes.Admin.OrderStatusRoute)]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] AdminUpdateOrderStatusRequest request, CancellationToken ct)
    {
        var result = await _adminOrderService.UpdateStatusAsync(id, request, ct);
        return result.ToOkResult();
    }
}
