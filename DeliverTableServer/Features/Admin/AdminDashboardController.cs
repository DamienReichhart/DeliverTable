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
public class AdminDashboardController(IAdminDashboardService adminDashboardService) : ControllerBase
{
    private readonly IAdminDashboardService _adminDashboardService = adminDashboardService;

    [HttpGet(ApiRoutes.Admin.DashboardRoute)]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        ServiceResult<AdminDashboardStatsResponse> result = await _adminDashboardService.GetStatsAsync(ct);
        return result.ToOkResult();
    }

    [HttpGet(ApiRoutes.Admin.DashboardAnalyticsRoute)]
    public async Task<IActionResult> GetAnalytics(CancellationToken ct)
    {
        ServiceResult<AdminDashboardAnalyticsResponse> result = await _adminDashboardService.GetAnalyticsAsync(ct);
        return result.ToOkResult();
    }
}
