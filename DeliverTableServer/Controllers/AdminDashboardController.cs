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
public class AdminDashboardController(IAdminDashboardService adminDashboardService) : ControllerBase
{
    private readonly IAdminDashboardService _adminDashboardService = adminDashboardService;

    [HttpGet(ApiRoutes.Admin.DashboardRoute)]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        var result = await _adminDashboardService.GetStatsAsync(ct);
        return result.ToOkResult();
    }
}
