using Microsoft.AspNetCore.Mvc;
using SevilAI.Application.DTOs;
using SevilAI.Application.Interfaces;

namespace SevilAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EstimateController : ControllerBase
{
    private readonly IEffortEstimationService _estimationService;
    private readonly ILogger<EstimateController> _logger;

    public EstimateController(
        IEffortEstimationService estimationService,
        ILogger<EstimateController> logger)
    {
        _estimationService = estimationService;
        _logger = logger;
    }

    /// <summary>
    /// Estimate project effort based on Sevil's skill profile
    /// </summary>
    /// <param name="request">Project description and requirements</param>
    /// <returns>Detailed effort estimation with phases, assumptions, and risks</returns>
    [HttpPost]
    [ProducesResponseType(typeof(EstimateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EstimateResponse>> Estimate([FromBody] EstimateRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            _logger.LogInformation("Received estimation request for: {Description}",
                request.ProjectDescription.Length > 100
                    ? request.ProjectDescription[..100] + "..."
                    : request.ProjectDescription);

            var response = await _estimationService.EstimateAsync(request, cancellationToken);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing estimation request");
            return StatusCode(500, new { error = "An error occurred while processing your estimation request" });
        }
    }

    /// <summary>
    /// Get estimation guidelines and example requests
    /// </summary>
    [HttpGet("guidelines")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public ActionResult GetGuidelines()
    {
        return Ok(new
        {
            methodology = "PERT-based estimation with skill profile weighting",
            factors = new
            {
                techFamiliarity = "Adjusts estimates based on Sevil's proficiency with requested technologies",
                projectComplexity = "Analyzes features and integrations to determine base effort",
                constraints = "High availability, security, and compliance requirements increase estimates"
            },
            exampleRequest = new EstimateRequest
            {
                ProjectDescription = "Build a REST API for inventory management with authentication",
                RequiredFeatures = new List<string>
                {
                    "User authentication",
                    "CRUD operations for products",
                    "Inventory tracking",
                    "Report generation"
                },
                TechStack = new List<string> { ".NET 8", "PostgreSQL", "Docker" },
                Constraints = new List<string> { "Must be scalable", "Security compliance required" },
                DetailedBreakdown = true
            },
            ndaNote = "All estimates are based on Sevil's publicly shareable skill profile. Specific past project timelines and proprietary methodologies cannot be disclosed due to NDA/company policies."
        });
    }
}
