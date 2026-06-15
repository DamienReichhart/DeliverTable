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
public class AdminRatingController(IAdminRatingService adminRatingService) : ControllerBase
{
    private readonly IAdminRatingService _adminRatingService = adminRatingService;

    [HttpGet(ApiRoutes.Admin.RestaurantRatingsRoute)]
    public async Task<IActionResult> GetRestaurantRatings(CancellationToken ct)
    {
        ServiceResult<List<AdminRestaurantRatingResponse>> result = await _adminRatingService.GetRestaurantRatingsAsync(ct);
        return result.ToOkResult();
    }

    [HttpDelete(ApiRoutes.Admin.RatingByIdRoute)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        ServiceResult result = await _adminRatingService.DeleteAsync(id, ct);
        return result.ToNoContentResult();
    }
}
