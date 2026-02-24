using DeliverTableServer.Services.Interfaces;
using DeliverTableSharedLibrary.Constants;
using DeliverTableSharedLibrary.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace DeliverTableServer.Controllers;

/// <summary>
///     Health check endpoint for liveness/readiness probes and monitoring.
/// </summary>
[ApiController]
[Route(ApiRoutes.Health)]
public class HealthController(IHealthService healthService) : ControllerBase
{
    private readonly IHealthService _healthService = healthService ?? throw new ArgumentNullException(nameof(healthService));

    /// <summary>
    ///     Returns service health status for liveness/readiness probes and monitoring.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with current status and UTC timestamp.</returns>
    [HttpGet(Name = "GetHealth")]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var health = await _healthService.GetHealthAsync(cancellationToken);
        return Ok(health);
    }
}