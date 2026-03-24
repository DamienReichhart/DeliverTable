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
public class AdminRatingController(IAdminRatingService adminRatingService) : ControllerBase
{
    private readonly IAdminRatingService _adminRatingService = adminRatingService;

    [HttpGet(ApiRoutes.Admin.RestaurantRatingsRoute)]
    public async Task<IActionResult> GetRestaurantRatings(CancellationToken ct)
    {
        var result = await _adminRatingService.GetRestaurantRatingsAsync(ct);
        return result.ToOkResult();
    }

    [HttpGet(ApiRoutes.Admin.CustomerRatingsRoute)]
    public async Task<IActionResult> GetCustomerRatings(CancellationToken ct)
    {
        var result = await _adminRatingService.GetCustomerRatingsAsync(ct);
        return result.ToOkResult();
    }

    [HttpDelete(ApiRoutes.Admin.RatingByIdRoute)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _adminRatingService.DeleteAsync(id, ct);
        return result.ToNoContentResult();
    }
}
