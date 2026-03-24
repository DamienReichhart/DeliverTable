using DeliverTableServer.Extensions;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeliverTableServer.Controllers;

[ApiController]
[Route(ApiRoutes.Admin.Base)]
[Authorize(Roles = nameof(UserRole.Administrator))]
public class AdminEventController(IAdminEventService adminEventService) : ControllerBase
{
    private readonly IAdminEventService _adminEventService = adminEventService;

    [HttpGet(ApiRoutes.Admin.EventsRoute)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _adminEventService.GetAllAsync(ct);
        return result.ToOkResult();
    }

    [HttpGet(ApiRoutes.Admin.EventByIdRoute)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _adminEventService.GetByIdAsync(id, ct);
        return result.ToOkResult();
    }

    [HttpPost(ApiRoutes.Admin.EventsRoute)]
    public async Task<IActionResult> Create([FromBody] AdminCreateEventRequest request, CancellationToken ct)
    {
        var result = await _adminEventService.CreateAsync(request, ct);
        if (!result.IsSuccess)
            return result.Error!.ToErrorResult();

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut(ApiRoutes.Admin.EventByIdRoute)]
    public async Task<IActionResult> Update(int id, [FromBody] AdminUpdateEventRequest request, CancellationToken ct)
    {
        var result = await _adminEventService.UpdateAsync(id, request, ct);
        return result.ToOkResult();
    }

    [HttpDelete(ApiRoutes.Admin.EventByIdRoute)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _adminEventService.DeleteAsync(id, ct);
        return result.ToNoContentResult();
    }
}
