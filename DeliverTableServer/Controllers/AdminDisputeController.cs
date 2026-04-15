using DeliverTableServer.Extensions;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos.Dispute;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeliverTableServer.Controllers;

[ApiController]
[Route(ApiRoutes.Admin.Base)]
[Authorize(Roles = nameof(UserRole.Administrator))]
public class AdminDisputeController(IDisputeService disputeService) : ControllerBase
{
    private readonly IDisputeService _disputeService = disputeService;

    [HttpGet(ApiRoutes.Admin.DisputesRoute)]
    public async Task<IActionResult> List([FromQuery] DisputeAdminFilter filter, CancellationToken ct)
    {
        var result = await _disputeService.ListForAdminAsync(filter, ct);
        return result.ToOkResult();
    }

    [HttpGet(ApiRoutes.Admin.DisputeByIdRoute)]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var result = await _disputeService.GetAdminDetailAsync(id, ct);
        return result.ToOkResult();
    }
}
