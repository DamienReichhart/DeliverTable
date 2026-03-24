using System.Security.Claims;
using DeliverTableServer.Extensions;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos.RestaurantAccount;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeliverTableServer.Controllers;

[ApiController]
[Route(ApiRoutes.RestaurantAccount.BaseRoute)]
[Authorize(Roles = nameof(UserRole.RestaurantOwner))]
public class RestaurantAccountController(IRestaurantAccountService accountService) : ControllerBase
{
    private readonly IRestaurantAccountService _accountService = accountService;

    [HttpGet]
    public async Task<IActionResult> GetAccount([FromRoute] int id, [FromQuery] TransactionQuery query, CancellationToken ct)
    {
        if (!TryGetUserId(out int userId)) return Unauthorized();

        var result = await _accountService.GetAccountAsync(id, userId, query, ct);
        return result.ToOkResult();
    }

    [HttpPost(ApiRoutes.RestaurantAccount.WithdrawRoute)]
    public async Task<IActionResult> Withdraw([FromRoute] int id, [FromBody] WithdrawRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out int userId)) return Unauthorized();

        var result = await _accountService.WithdrawAsync(id, userId, request, ct);
        return result.ToOkResult();
    }

    private bool TryGetUserId(out int userId)
    {
        return int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out userId);
    }
}
