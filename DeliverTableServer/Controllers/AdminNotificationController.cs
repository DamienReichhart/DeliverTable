using DeliverTableServer.Extensions;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Constants.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeliverTableServer.Controllers;

[ApiController]
[Route(ApiRoutes.Admin.Base)]
[Authorize(Roles = nameof(UserRole.Administrator))]
public class AdminNotificationController(IAdminNotificationService adminNotificationService) : ControllerBase
{
    private readonly IAdminNotificationService _adminNotificationService = adminNotificationService;

    [HttpGet(ApiRoutes.Admin.NotificationsRoute)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _adminNotificationService.GetAllAsync(ct);
        return result.ToOkResult();
    }

    [HttpDelete(ApiRoutes.Admin.NotificationByIdRoute)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _adminNotificationService.DeleteAsync(id, ct);
        return result.ToNoContentResult();
    }
}
