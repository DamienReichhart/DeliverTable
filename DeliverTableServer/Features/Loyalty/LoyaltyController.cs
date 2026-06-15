using DeliverTableServer.Common;
using DeliverTableServer.Extensions;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos.Loyalty;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeliverTableServer.Features.Loyalty;

[ApiController]
[Route(ApiRoutes.Loyalty.RestaurantBaseRoute)]
public class LoyaltyController(ILoyaltyService loyaltyService) : ControllerBase
{
    private readonly ILoyaltyService _loyaltyService = loyaltyService;

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.RestaurantOwner))]
    public async Task<IActionResult> CreateOrUpdate([FromRoute] int id, [FromBody] CreateLoyaltyProgramRequest request, CancellationToken ct)
    {
        if (!this.TryGetUserId(out int userId)) return Unauthorized();
        ServiceResult<LoyaltyProgramDto> result = await _loyaltyService.CreateOrUpdateProgramAsync(id, userId, request, ct);
        return result.ToOkResult();
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetProgram([FromRoute] int id, CancellationToken ct)
    {
        ServiceResult<LoyaltyProgramDto> result = await _loyaltyService.GetProgramAsync(id, ct);
        return result.ToOkResult();
    }

    [HttpGet(ApiRoutes.Loyalty.MyAccountRoute)]
    [Authorize(Roles = nameof(UserRole.Customer))]
    public async Task<IActionResult> GetMyAccount([FromRoute] int id, CancellationToken ct)
    {
        if (!this.TryGetUserId(out int userId)) return Unauthorized();
        ServiceResult<LoyaltyAccountDto> result = await _loyaltyService.GetMyAccountAsync(id, userId, ct);
        return result.ToOkResult();
    }

}
