using DeliverTableServer.Common;
using DeliverTableServer.Extensions;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.CommissionStatement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeliverTableServer.Features.CommissionStatement;

[ApiController]
[Route(ApiRoutes.CommissionStatement.Base)]
[Authorize]
public class CommissionStatementController(ICommissionStatementService service) : ControllerBase
{
    [HttpGet(ApiRoutes.CommissionStatement.RestaurantListRoute)]
    [Authorize(Roles = nameof(UserRole.RestaurantOwner) + "," + nameof(UserRole.Administrator))]
    public async Task<IActionResult> GetForRestaurant(
        [FromRoute] int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        if (!this.TryGetUserId(out int userId)) return Unauthorized();
        bool isAdmin = User.IsInRole(nameof(UserRole.Administrator));
        ServiceResult<PaginatedResult<AdminCommissionStatementRowDto>> result = await service.ListForRestaurantAsync(id, userId, isAdmin, page, pageSize, ct);
        return result.ToOkResult();
    }

    [HttpGet(ApiRoutes.CommissionStatement.DownloadRoute)]
    public async Task<IActionResult> Download([FromRoute] int id, CancellationToken ct)
    {
        if (!this.TryGetUserId(out int userId)) return Unauthorized();
        bool isAdmin = User.IsInRole(nameof(UserRole.Administrator));
        bool isRestaurantOwner = User.IsInRole(nameof(UserRole.RestaurantOwner));
        ServiceResult<(byte[] Pdf, string FileName)> result = await service.GetPdfForOwnerAsync(id, userId, isAdmin, isRestaurantOwner, ct);
        if (!result.IsSuccess) return result.Error!.ToErrorResult();
        (byte[]? pdf, string? fileName) = result.Value!;
        return File(pdf, "application/pdf", fileName);
    }
}
