using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SevilAI.Application.DTOs;
using SevilAI.Application.Interfaces;
using SevilAI.Domain.Interfaces;

namespace SevilAI.Application.Services;

public class HealthService : IHealthService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IChunkRepository _chunkRepository;
    private readonly ISkillRepository _skillRepository;
    private readonly IExperienceRepository _experienceRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IEmbeddingService _embeddingService;
    private readonly ITextGenerator _textGenerator;
    private readonly ILogger<HealthService> _logger;

    public HealthService(
        IDocumentRepository documentRepository,
        IChunkRepository chunkRepository,
        ISkillRepository skillRepository,
        IExperienceRepository experienceRepository,
        IProjectRepository projectRepository,
        IEmbeddingService embeddingService,
        ITextGenerator textGenerator,
        ILogger<HealthService> logger)
    {
        _documentRepository = documentRepository;
        _chunkRepository = chunkRepository;
        _skillRepository = skillRepository;
        _experienceRepository = experienceRepository;
        _projectRepository = projectRepository;
        _embeddingService = embeddingService;
        _textGenerator = textGenerator;
        _logger = logger;
    }

    public async Task<HealthResponse> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        var response = new HealthResponse
        {
            Version = "1.0.0",
            Timestamp = DateTime.UtcNow
        };

        // Check database
        response.Database = await CheckDatabaseHealthAsync(cancellationToken);

        // Check LLM
        response.LLM = new LLMHealth
        {
            Provider = _textGenerator.ProviderName,
            Model = _textGenerator.ModelName,
            Available = _textGenerator.ProviderName != "NoLLM"
        };

        // Check embedding service
        response.Embedding = new EmbeddingHealth
        {
            Model = _embeddingService.ModelName,
            Dimensions = _embeddingService.Dimensions,
            Available = true
        };

        // Get knowledge base stats
        response.KnowledgeBase = await GetKnowledgeBaseStatsAsync(cancellationToken);

        // Determine overall status
        response.Status = response.Database.Connected ? "healthy" : "unhealthy";

        return response;
    }

    private async Task<DatabaseHealth> CheckDatabaseHealthAsync(CancellationToken cancellationToken)
    {
        var health = new DatabaseHealth();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Simple query to check connection
            await _documentRepository.GetAllAsync(cancellationToken);
            health.Connected = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Database health check failed");
            health.Connected = false;
        }

        stopwatch.Stop();
        health.LatencyMs = (int)stopwatch.ElapsedMilliseconds;

        return health;
    }

    private async Task<KnowledgeBaseStats> GetKnowledgeBaseStatsAsync(CancellationToken cancellationToken)
    {
        var stats = new KnowledgeBaseStats();

        try
        {
            var documents = await _documentRepository.GetAllAsync(cancellationToken);
            stats.TotalDocuments = documents.Count();

            var skills = await _skillRepository.GetAllAsync(cancellationToken);
            stats.TotalSkills = skills.Count();

            var experiences = await _experienceRepository.GetAllAsync(cancellationToken);
            stats.TotalExperiences = experiences.Count();

            var projects = await _projectRepository.GetAllAsync(cancellationToken);
            stats.TotalProjects = projects.Count();

            // Estimate chunks from documents
            stats.TotalChunks = documents.Sum(d => d.Chunks?.Count ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get knowledge base stats");
        }

        return stats;
    }
}
