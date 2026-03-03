using DeliverTableSharedLibrary.Constants;
using Microsoft.AspNetCore.Mvc;

namespace DeliverTableServer.Controllers
{
    [ApiController]
    [Route(ApiRoutes.Test)]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public IActionResult Test()
        {
            throw new UnauthorizedAccessException("Chemin introuvable");
        }
    }
}