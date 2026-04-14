using DeliverTableServer.Repositories.Interfaces;
using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeliverTableServer.Controllers;


// [Authorize]
[ApiController]
[Route(ApiRoutes.Order.Base)]
public class ReclamationController(
    IReclamationService reclamationService
    ) : ControllerBase
{
    public async Task<IActionResult> Index()
    {
        return Ok(await reclamationService.GetAllReclamations());
    }
}