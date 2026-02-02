using Microsoft.AspNetCore.Mvc;
using SevilAI.Application.DTOs;
using SevilAI.Application.Interfaces;

namespace SevilAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AskController : ControllerBase
{
    private readonly IQuestionAnsweringService _questionAnsweringService;
    private readonly ILogger<AskController> _logger;

    public AskController(
        IQuestionAnsweringService questionAnsweringService,
        ILogger<AskController> logger)
    {
        _questionAnsweringService = questionAnsweringService;
        _logger = logger;
    }

    /// <summary>
    /// Ask a question about Sevil's professional profile
    /// </summary>
    /// <param name="request">The question and configuration</param>
    /// <returns>Answer with sources and confidence score</returns>
    [HttpPost]
    [ProducesResponseType(typeof(AskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AskResponse>> Ask([FromBody] AskRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            _logger.LogInformation("Received question: {Question}", request.Question);

            var response = await _questionAnsweringService.AskAsync(request, cancellationToken);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing question: {Question}", request.Question);
            return StatusCode(500, new { error = "An error occurred while processing your question" });
        }
    }

    /// <summary>
    /// Example questions to try
    /// </summary>
    [HttpGet("examples")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public ActionResult GetExamples()
    {
        return Ok(new
        {
            examples = new[]
            {
                new { question = "What jobs did Sevil do?", description = "Career history and work experience" },
                new { question = "Which domains is Sevil strong in?", description = "Technical skills and expertise" },
                new { question = "What technologies does Sevil work with?", description = "Technology stack and tools" },
                new { question = "Tell me about Sevil's projects", description = "Personal and enterprise projects" },
                new { question = "What is Sevil's work style?", description = "Professional character and traits" },
                new { question = "What are Sevil's career goals?", description = "Long-term professional objectives" }
            },
            usage = new
            {
                endpoint = "POST /api/ask",
                body = new
                {
                    question = "your question here",
                    topK = 5,
                    minSimilarity = 0.3,
                    useLLM = true,
                    includeSources = true
                }
            }
        });
    }
}
