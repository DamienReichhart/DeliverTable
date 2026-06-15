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
public class AdminNotificationController(IAdminNotificationService adminNotificationService) : ControllerBase
{
    private readonly IAdminNotificationService _adminNotificationService = adminNotificationService;

    [HttpGet(ApiRoutes.Admin.NotificationsRoute)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        ServiceResult<List<AdminNotificationResponse>> result = await _adminNotificationService.GetAllAsync(ct);
        return result.ToOkResult();
    }

    [HttpDelete(ApiRoutes.Admin.NotificationByIdRoute)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        ServiceResult result = await _adminNotificationService.DeleteAsync(id, ct);
        return result.ToNoContentResult();
    }
}
