using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SevilAI.Application.DTOs;
using SevilAI.Application.Services;
using SevilAI.Domain.Entities;
using SevilAI.Domain.Interfaces;
using SevilAI.Domain.ValueObjects;
using Xunit;

namespace SevilAI.Tests;

public class QuestionAnsweringServiceTests
{
    private readonly Mock<IEmbeddingService> _embeddingService;
    private readonly Mock<IEmbeddingRepository> _embeddingRepository;
    private readonly Mock<ITextGenerator> _textGenerator;
    private readonly Mock<IQueryLogRepository> _queryLogRepository;
    private readonly Mock<ISkillRepository> _skillRepository;
    private readonly Mock<IExperienceRepository> _experienceRepository;
    private readonly Mock<IProjectRepository> _projectRepository;
    private readonly Mock<ILogger<QuestionAnsweringService>> _logger;
    private readonly QuestionAnsweringService _sut;

    public QuestionAnsweringServiceTests()
    {
        _embeddingService = new Mock<IEmbeddingService>();
        _embeddingRepository = new Mock<IEmbeddingRepository>();
        _textGenerator = new Mock<ITextGenerator>();
        _queryLogRepository = new Mock<IQueryLogRepository>();
        _skillRepository = new Mock<ISkillRepository>();
        _experienceRepository = new Mock<IExperienceRepository>();
        _projectRepository = new Mock<IProjectRepository>();
        _logger = new Mock<ILogger<QuestionAnsweringService>>();

        // Default setups
        _embeddingService.Setup(x => x.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new float[384]);

        _embeddingService.Setup(x => x.ModelName).Returns("test-model");

        _textGenerator.Setup(x => x.ProviderName).Returns("NoLLM");
        _textGenerator.Setup(x => x.ModelName).Returns("none");

        _queryLogRepository.Setup(x => x.AddAsync(It.IsAny<QueryLog>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((QueryLog log, CancellationToken _) => log);

        _sut = new QuestionAnsweringService(
            _embeddingService.Object,
            _embeddingRepository.Object,
            _textGenerator.Object,
            _queryLogRepository.Object,
            _skillRepository.Object,
            _experienceRepository.Object,
            _projectRepository.Object,
            _logger.Object);
    }

    [Fact]
    public async Task AskAsync_WithNoMatchingChunks_ReturnsNotFoundMessage()
    {
        // Arrange
        var request = new AskRequest
        {
            Question = "What is Sevil's favorite color?",
            TopK = 5
        };

        _embeddingRepository.Setup(x => x.SearchSimilarAsync(
                It.IsAny<float[]>(), It.IsAny<int>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RetrievedChunk>());

        // Act
        var result = await _sut.AskAsync(request);

        // Assert
        result.Answer.Should().Contain("Not found");
        result.ConfidenceScore.Should().Be(0);
        result.Sources.Should().BeEmpty();
    }

    [Fact]
    public async Task AskAsync_WithMatchingChunks_ReturnsAnswerWithSources()
    {
        // Arrange
        var request = new AskRequest
        {
            Question = "What jobs did Sevil do?",
            TopK = 3,
            IncludeSources = true,
            UseLLM = false
        };

        var chunks = new List<RetrievedChunk>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "Worked at CTECH as Software Engineer", "CTECH Experience", "experience", 0.85),
            new(Guid.NewGuid(), Guid.NewGuid(), "Previous role at SAMTEK as Electrical Engineer", "SAMTEK Experience", "experience", 0.75)
        };

        _embeddingRepository.Setup(x => x.SearchSimilarAsync(
                It.IsAny<float[]>(), It.IsAny<int>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chunks);

        // Act
        var result = await _sut.AskAsync(request);

        // Assert
        result.Answer.Should().NotBeEmpty();
        result.Answer.Should().NotContain("Not found");
        result.Sources.Should().HaveCount(2);
        result.ConfidenceScore.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AskAsync_WithLLMEnabled_UsesLLMGeneration()
    {
        // Arrange
        var request = new AskRequest
        {
            Question = "What jobs did Sevil do?",
            UseLLM = true
        };

        var chunks = new List<RetrievedChunk>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "Worked at CTECH", "CTECH", "experience", 0.85)
        };

        _embeddingRepository.Setup(x => x.SearchSimilarAsync(
                It.IsAny<float[]>(), It.IsAny<int>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chunks);

        _textGenerator.Setup(x => x.ProviderName).Returns("Groq");
        _textGenerator.Setup(x => x.ModelName).Returns("llama-3.3-70b");
        _textGenerator.Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GenerationResult("LLM generated answer about Sevil's jobs", 100, 50, true));

        // Act
        var result = await _sut.AskAsync(request);

        // Assert
        result.Answer.Should().Contain("LLM generated answer");
        result.GenerationMode.Should().Be("LLM");
        result.Metadata.TokensUsed.Should().Be(150);
    }

    [Fact]
    public async Task AskAsync_LogsQuery()
    {
        // Arrange
        var request = new AskRequest
        {
            Question = "Test question"
        };

        _embeddingRepository.Setup(x => x.SearchSimilarAsync(
                It.IsAny<float[]>(), It.IsAny<int>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RetrievedChunk>());

        // Act
        await _sut.AskAsync(request);

        // Assert
        _queryLogRepository.Verify(x => x.AddAsync(
            It.Is<QueryLog>(log => log.QueryText == "Test question"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AskAsync_ReturnsLatencyMs()
    {
        // Arrange
        var request = new AskRequest
        {
            Question = "Test question"
        };

        _embeddingRepository.Setup(x => x.SearchSimilarAsync(
                It.IsAny<float[]>(), It.IsAny<int>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RetrievedChunk>());

        // Act
        var result = await _sut.AskAsync(request);

        // Assert
        result.LatencyMs.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task AskAsync_WithJobQuestion_FormatsExperienceResponse()
    {
        // Arrange
        var request = new AskRequest
        {
            Question = "What jobs did Sevil do?",
            UseLLM = false,
            IncludeSources = true
        };

        var chunks = new List<RetrievedChunk>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "Software Engineer at CTECH", "CTECH Career", "experience", 0.9)
        };

        _embeddingRepository.Setup(x => x.SearchSimilarAsync(
                It.IsAny<float[]>(), It.IsAny<int>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chunks);

        // Act
        var result = await _sut.AskAsync(request);

        // Assert
        result.Answer.Should().Contain("Work Experience");
    }

    [Fact]
    public async Task AskAsync_WithSkillQuestion_FormatsSkillResponse()
    {
        // Arrange
        var request = new AskRequest
        {
            Question = "Which domains is Sevil strong in?",
            UseLLM = false
        };

        var chunks = new List<RetrievedChunk>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), ".NET, C#, System Integration", "Skills", "skill", 0.9)
        };

        _embeddingRepository.Setup(x => x.SearchSimilarAsync(
                It.IsAny<float[]>(), It.IsAny<int>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chunks);

        // Act
        var result = await _sut.AskAsync(request);

        // Assert
        result.Answer.Should().Contain("Skills");
    }

    [Fact]
    public async Task AskAsync_ExcludesSourcesWhenNotRequested()
    {
        // Arrange
        var request = new AskRequest
        {
            Question = "Test question",
            IncludeSources = false
        };

        var chunks = new List<RetrievedChunk>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "Content", "Title", "type", 0.9)
        };

        _embeddingRepository.Setup(x => x.SearchSimilarAsync(
                It.IsAny<float[]>(), It.IsAny<int>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chunks);

        // Act
        var result = await _sut.AskAsync(request);

        // Assert
        result.Sources.Should().BeEmpty();
    }
}
