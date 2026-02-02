using Microsoft.AspNetCore.Mvc;
using SevilAI.Application.DTOs;
using SevilAI.Application.Interfaces;

namespace SevilAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly IHealthService _healthService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        IHealthService healthService,
        ILogger<HealthController> logger)
    {
        _healthService = healthService;
        _logger = logger;
    }

    /// <summary>
    /// Get system health status and knowledge base statistics
    /// </summary>
    /// <returns>Health status with component details</returns>
    [HttpGet]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<HealthResponse>> GetHealth(CancellationToken cancellationToken)
    {
        try
        {
            var health = await _healthService.GetHealthAsync(cancellationToken);

            if (health.Status == "unhealthy")
            {
                return StatusCode(503, health);
            }

            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking health");

            return StatusCode(503, new HealthResponse
            {
                Status = "unhealthy",
                Timestamp = DateTime.UtcNow,
                Database = new DatabaseHealth { Connected = false }
            });
        }
    }

    /// <summary>
    /// Simple liveness probe
    /// </summary>
    [HttpGet("live")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult Live()
    {
        return Ok(new { status = "alive", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Readiness probe (checks database connectivity)
    /// </summary>
    [HttpGet("ready")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult> Ready(CancellationToken cancellationToken)
    {
        try
        {
            var health = await _healthService.GetHealthAsync(cancellationToken);

            if (health.Database.Connected)
            {
                return Ok(new { status = "ready", timestamp = DateTime.UtcNow });
            }

            return StatusCode(503, new { status = "not ready", reason = "database not connected" });
        }
        catch
        {
            return StatusCode(503, new { status = "not ready", reason = "health check failed" });
        }
    }
}
