using DeliverTableServer.Extensions;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Constants.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeliverTableServer.Controllers;

[ApiController]
[Route(ApiRoutes.Payment.Base)]
[Authorize]
public class PaymentController(IPaymentService paymentService) : ControllerBase
{
    private readonly IPaymentService _paymentService = paymentService;

    [HttpPost(ApiRoutes.Payment.CancelRoute)]
    [Authorize(Roles = nameof(UserRole.Customer))]
    public async Task<IActionResult> Cancel([FromRoute] int orderId, CancellationToken ct)
    {
        if (!this.TryGetUserId(out int _)) return Unauthorized();

        var result = await _paymentService.CancelAuthorizationAsync(orderId, ct);
        return result.ToNoContentResult();
    }
}
