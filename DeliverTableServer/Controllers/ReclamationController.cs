using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Dtos.Reclamation;
using Microsoft.AspNetCore.Mvc;
using DeliverTableServer.Services.Interfaces;
using DeliverTableServer.Mappers;
using Microsoft.AspNetCore.Authorization;

namespace DeliverTableServer.Controllers;

[ApiController]
[Route(ApiRoutes.Reclamation.Base)]
[Authorize]
public class ReclamationController(
    IReclamationService reclamationService
    ) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] ReclamationQuery query)
    {
        return Ok(await reclamationService.GetAllReclamations(query));
    }

    [HttpGet(ApiRoutes.Reclamation.ByIdRoute)]
    public async Task<IActionResult> GetById(int id)
    {
        return Ok(await reclamationService.GetReclamationById(id));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromForm] CreateReclamationDto reclamation)
    {
        IFormFileCollection images = Request.Form.Files;
        Reclamation created = await reclamationService.CreateReclamation(reclamation, images);
        return Ok(created.ToDto());
    }

    [HttpPut(ApiRoutes.Reclamation.ByIdRoute)]
    public async Task<IActionResult> Update(int id, Reclamation reclamation)
    {
        return Ok(await reclamationService.UpdateReclamation(id, reclamation));
    }

    [HttpDelete(ApiRoutes.Reclamation.ByIdRoute)]
    public async Task<IActionResult> Delete(int id)
    {
        await reclamationService.DeleteReclamation(id);
        return NoContent();
    }

    [HttpGet(ApiRoutes.Reclamation.ByUserRoute)]
    public async Task<IActionResult> GetByUserId(int userId)
    {
        return Ok(await reclamationService.GetReclamationsByUser(userId));
    }

    [HttpGet(ApiRoutes.Reclamation.ByOrderRoute)]
    public async Task<IActionResult> GetByOrderId(int orderId)
    {
        return Ok(await reclamationService.GetReclamationsByOrderId(orderId));
    }

    [HttpGet(ApiRoutes.Reclamation.ByRestaurantRoute)]
    public async Task<IActionResult> GetByRestaurantId(int restaurantId)
    {
        return Ok(await reclamationService.GetReclamationsByRestaurant(restaurantId));
    }
}