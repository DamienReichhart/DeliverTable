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
public class AdminEventController(IAdminEventService adminEventService) : ControllerBase
{
    private readonly IAdminEventService _adminEventService = adminEventService;

    [HttpGet(ApiRoutes.Admin.EventsRoute)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        ServiceResult<List<AdminEventResponse>> result = await _adminEventService.GetAllAsync(ct);
        return result.ToOkResult();
    }

    [HttpGet(ApiRoutes.Admin.EventByIdRoute)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        ServiceResult<AdminEventResponse> result = await _adminEventService.GetByIdAsync(id, ct);
        return result.ToOkResult();
    }

    [HttpPost(ApiRoutes.Admin.EventsRoute)]
    public async Task<IActionResult> Create([FromBody] AdminCreateEventRequest request, CancellationToken ct)
    {
        ServiceResult<AdminEventResponse> result = await _adminEventService.CreateAsync(request, ct);
        return result.ToCreatedResult(nameof(GetById), v => new { id = v.Id });
    }

    [HttpPut(ApiRoutes.Admin.EventByIdRoute)]
    public async Task<IActionResult> Update(int id, [FromBody] AdminUpdateEventRequest request, CancellationToken ct)
    {
        ServiceResult<AdminEventResponse> result = await _adminEventService.UpdateAsync(id, request, ct);
        return result.ToOkResult();
    }

    [HttpDelete(ApiRoutes.Admin.EventByIdRoute)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        ServiceResult result = await _adminEventService.DeleteAsync(id, ct);
        return result.ToNoContentResult();
    }
}
