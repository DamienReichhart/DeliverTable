using DeliverTableServer.Common;
using DeliverTableServer.Extensions;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos;
using DeliverTableSharedLibrary.Dtos.Dispute;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeliverTableServer.Features.Dispute;

[ApiController]
[Route(ApiRoutes.Dispute.Base)]
[Authorize]
public class DisputeController(IDisputeService disputeService) : ControllerBase
{
    private readonly IDisputeService _disputeService = disputeService;

    [HttpGet(ApiRoutes.Dispute.RestaurantListRoute)]
    [Authorize(Roles = nameof(UserRole.RestaurantOwner) + "," + nameof(UserRole.Administrator))]
    public async Task<IActionResult> GetForRestaurant(
        [FromRoute] int id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (!this.TryGetUserId(out int userId)) return Unauthorized();
        bool isAdmin = User.IsInRole(nameof(UserRole.Administrator));
        ServiceResult<PaginatedResult<DisputeRowDto>> result = await _disputeService.ListForRestaurantAsync(id, page, pageSize, userId, isAdmin, ct);
        return result.ToOkResult();
    }
}
