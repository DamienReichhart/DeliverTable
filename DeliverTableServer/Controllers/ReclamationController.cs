using DeliverTableServer.Extensions;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Constants.Enums;
using DeliverTableSharedLibrary.Dtos.Reclamation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeliverTableServer.Controllers;


// [Authorize]
[ApiController]
[Route(ApiRoutes.Reclamation.Base)]
public class ReclamationController(
    IReclamationService reclamationService
    ) : ControllerBase
{
    private readonly IReclamationService _reclamationService = reclamationService;

    [HttpGet]
    [Authorize(Roles = nameof(UserRole.Administrator))]
    public async Task<IActionResult> Index([FromQuery] ReclamationQuery query)
    {
        var result = await _reclamationService.GetAllReclamations(query);
        return result.ToOkResult();
    }

    [HttpGet(ApiRoutes.Reclamation.ByIdRoute)]
    [Authorize(Roles = nameof(UserRole.Administrator))]
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        var result = await _reclamationService.GetReclamationById(id);
        return result.ToOkResult();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromForm] CreateReclamationDto reclamation)
    {
        var result = await _reclamationService.CreateReclamation(reclamation, Request.Form.Files);
        return result.ToOkResult();
    }

    [HttpPut(ApiRoutes.Reclamation.ByIdRoute)]
    [Authorize(Roles = nameof(UserRole.Administrator))]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateReclamationDto reclamation)
    {
        var result = await _reclamationService.UpdateReclamation(id, reclamation);
        return result.ToOkResult();
    }

    [HttpDelete(ApiRoutes.Reclamation.ByIdRoute)]
    [Authorize(Roles = nameof(UserRole.Administrator))]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        var result = await _reclamationService.DeleteReclamation(id);
        return result.ToNoContentResult();
    }

    [HttpGet(ApiRoutes.Reclamation.ByUserRoute)]
    [Authorize(Roles = nameof(UserRole.Administrator))]
    public async Task<IActionResult> GetByUserId([FromRoute] int userId)
    {
        var result = await _reclamationService.GetReclamationsByUser(userId);
        return result.ToOkResult();
    }

    [HttpGet(ApiRoutes.Reclamation.ByOrderRoute)]
    public async Task<IActionResult> GetByOrderId([FromRoute] int orderId)
    {
        var result = await _reclamationService.GetReclamationsByOrderId(orderId);
        return result.ToOkResult();
    }

    [HttpGet(ApiRoutes.Reclamation.ByRestaurantRoute)]
    [Authorize(Roles = nameof(UserRole.Administrator))]
    public async Task<IActionResult> GetByRestaurantId([FromRoute] int restaurantId)
    {
        var result = await _reclamationService.GetReclamationsByRestaurant(restaurantId);
        return result.ToOkResult();
    }

    [HttpGet(ApiRoutes.Reclamation.MyRestaurantRoute)]
    [Authorize(Roles = nameof(UserRole.RestaurantOwner))]
    public async Task<IActionResult> GetMyRestaurantReclamations()
    {
        if (!this.TryGetUserId(out int ownerId)) return Unauthorized();
        var result = await _reclamationService.GetReclamationsByRestaurantOwner(ownerId);
        return result.ToOkResult();
    }

    [HttpPost(ApiRoutes.Reclamation.RefundRoute)]
    [Authorize(Roles = nameof(UserRole.RestaurantOwner))]
    public async Task<IActionResult> Refund([FromRoute] int id, [FromBody] RefundReclamationDto dto)
    {
        if (!this.TryGetUserId(out int ownerId)) return Unauthorized();
        var result = await _reclamationService.RefundReclamation(id, ownerId, dto);
        return result.ToOkResult();
    }

    [HttpPatch(ApiRoutes.Reclamation.ResolveRoute)]
    [Authorize(Roles = nameof(UserRole.RestaurantOwner))]
    public async Task<IActionResult> Resolve([FromRoute] int id)
    {
        if (!this.TryGetUserId(out int ownerId)) return Unauthorized();
        var result = await _reclamationService.ResolveReclamation(id, ownerId);
        return result.ToOkResult();
    }

    [HttpPatch(ApiRoutes.Reclamation.ContestRoute)]
    [Authorize(Roles = nameof(UserRole.Customer))]
    public async Task<IActionResult> Contest([FromRoute] int id)
    {
        if (!this.TryGetUserId(out int customerId)) return Unauthorized();
        var result = await _reclamationService.ContestReclamation(id, customerId);
        return result.ToOkResult();
    }

    [HttpPatch(ApiRoutes.Reclamation.CompleteRoute)]
    [Authorize(Roles = nameof(UserRole.Administrator))]
    public async Task<IActionResult> Complete([FromRoute] int id)
    {
        var result = await _reclamationService.CompleteReclamation(id);
        return result.ToOkResult();
    }
}
