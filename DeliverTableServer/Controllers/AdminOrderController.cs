using DeliverTableServer.Extensions;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos.Admin;
using DeliverTableSharedLibrary.Dtos.Payment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeliverTableServer.Controllers;

[ApiController]
[Route(ApiRoutes.Admin.Base)]
[Authorize(Roles = nameof(UserRole.Administrator))]
public class AdminOrderController(IAdminOrderService adminOrderService, IPaymentService paymentService) : ControllerBase
{
    private readonly IAdminOrderService _adminOrderService = adminOrderService;
    private readonly IPaymentService _paymentService = paymentService;

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

    [HttpPost(ApiRoutes.Admin.OrderRefundRoute)]
    [Authorize(Roles = nameof(UserRole.Administrator))]
    public async Task<IActionResult> RefundOrder(int id, [FromBody] AdminRefundRequest request, CancellationToken ct)
    {
        if (!this.TryGetUserId(out int adminId)) return Unauthorized();
        var result = await _paymentService.RefundAsync(id, request.Amount, request.Reason, adminId, ct);
        return result.IsSuccess ? Ok(result.Value) : result.Error!.ToErrorResult();
    }
}
