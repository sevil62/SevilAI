using Microsoft.AspNetCore.Mvc;
using SevilAI.Application.DTOs;
using SevilAI.Application.Interfaces;

namespace SevilAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SeedController : ControllerBase
{
    private readonly ISeedingService _seedingService;
    private readonly ILogger<SeedController> _logger;

    public SeedController(
        ISeedingService seedingService,
        ILogger<SeedController> logger)
    {
        _seedingService = seedingService;
        _logger = logger;
    }

    /// <summary>
    /// Seed the knowledge base with JSON data
    /// </summary>
    /// <param name="request">Optional JSON data and clear flag</param>
    /// <returns>Seeding results with counts</returns>
    [HttpPost]
    [ProducesResponseType(typeof(SeedResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SeedResponse>> Seed([FromBody] SeedRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Received seed request. ClearExisting: {Clear}", request.ClearExisting);

            SeedResponse response;

            if (!string.IsNullOrWhiteSpace(request.JsonData))
            {
                // Seed from provided JSON
                response = await _seedingService.SeedFromJsonAsync(
                    request.JsonData,
                    request.ClearExisting,
                    cancellationToken);
            }
            else
            {
                // Seed from embedded resource
                response = await _seedingService.SeedFromEmbeddedResourceAsync(
                    request.ClearExisting,
                    cancellationToken);
            }

            if (response.Success)
            {
                _logger.LogInformation(
                    "Seeding completed successfully: {Docs} documents, {Chunks} chunks, {Embeddings} embeddings",
                    response.DocumentsCreated, response.ChunksCreated, response.EmbeddingsCreated);
            }
            else
            {
                _logger.LogWarning("Seeding completed with errors: {Errors}", string.Join(", ", response.Errors));
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during seeding");
            return StatusCode(500, new { error = "An error occurred while seeding the knowledge base" });
        }
    }

    /// <summary>
    /// Seed from the embedded default knowledge base
    /// </summary>
    [HttpPost("default")]
    [ProducesResponseType(typeof(SeedResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SeedResponse>> SeedDefault([FromQuery] bool clearExisting = false, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Seeding from default embedded resource");

            var response = await _seedingService.SeedFromEmbeddedResourceAsync(clearExisting, cancellationToken);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during default seeding");
            return StatusCode(500, new { error = "An error occurred while seeding from default data" });
        }
    }
}
