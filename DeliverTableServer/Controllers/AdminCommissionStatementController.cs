using DeliverTableServer.Extensions;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeliverTableServer.Controllers;

[ApiController]
[Route(ApiRoutes.Admin.Base)]
[Authorize(Roles = nameof(UserRole.Administrator))]
public class AdminCommissionStatementController(
    ICommissionStatementService commissionStatementService) : ControllerBase
{
    // Test seam — overridden in unit tests; in production reads UtcNow.
    public DateTime? UtcNowOverride { get; set; }

    private static readonly TimeZoneInfo ParisTz =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris");

    [HttpPost(ApiRoutes.Admin.CommissionStatementsRunRoute)]
    public async Task<IActionResult> Run(
        [FromBody] CommissionStatementsRunRequest? body, CancellationToken ct)
    {
        int year, month;
        if (body?.Year is int y && body.Month is int m)
        {
            year = y;
            month = m;
        }
        else
        {
            var nowUtc = UtcNowOverride ?? DateTime.UtcNow;
            var nowParis = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, ParisTz);
            var prev = nowParis.AddMonths(-1);
            year = prev.Year;
            month = prev.Month;
        }

        var result = await commissionStatementService.GenerateForPeriodAsync(year, month, ct);
        return result.ToOkResult();
    }
}
