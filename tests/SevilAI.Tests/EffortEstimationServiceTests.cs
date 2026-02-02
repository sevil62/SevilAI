using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SevilAI.Application.DTOs;
using SevilAI.Application.Services;
using SevilAI.Domain.Entities;
using SevilAI.Domain.Enums;
using SevilAI.Domain.Interfaces;
using Xunit;

namespace SevilAI.Tests;

public class EffortEstimationServiceTests
{
    private readonly Mock<ISkillRepository> _skillRepository;
    private readonly Mock<IExperienceRepository> _experienceRepository;
    private readonly Mock<IProjectRepository> _projectRepository;
    private readonly Mock<ITextGenerator> _textGenerator;
    private readonly Mock<ILogger<EffortEstimationService>> _logger;
    private readonly EffortEstimationService _sut;

    public EffortEstimationServiceTests()
    {
        _skillRepository = new Mock<ISkillRepository>();
        _experienceRepository = new Mock<IExperienceRepository>();
        _projectRepository = new Mock<IProjectRepository>();
        _textGenerator = new Mock<ITextGenerator>();
        _logger = new Mock<ILogger<EffortEstimationService>>();

        // Setup default returns
        _skillRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Skill>
            {
                new() { Name = ".NET", Category = SkillCategory.Language, ProficiencyLevel = ProficiencyLevel.Expert },
                new() { Name = "C#", Category = SkillCategory.Language, ProficiencyLevel = ProficiencyLevel.Expert },
                new() { Name = "PostgreSQL", Category = SkillCategory.Database, ProficiencyLevel = ProficiencyLevel.Advanced }
            });

        _experienceRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Experience>());

        _projectRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project>());

        _sut = new EffortEstimationService(
            _skillRepository.Object,
            _experienceRepository.Object,
            _projectRepository.Object,
            _textGenerator.Object,
            _logger.Object);
    }

    [Fact]
    public async Task EstimateAsync_WithSimpleProject_ReturnsReasonableEstimate()
    {
        // Arrange
        var request = new EstimateRequest
        {
            ProjectDescription = "Build a simple REST API with CRUD operations",
            RequiredFeatures = new List<string> { "CRUD operations" },
            TechStack = new List<string> { ".NET", "C#" }
        };

        // Act
        var result = await _sut.EstimateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.TotalEffort.MinDays.Should().BeGreaterThan(0);
        result.TotalEffort.MaxDays.Should().BeGreaterThanOrEqualTo(result.TotalEffort.MinDays);
        result.TotalEffort.RecommendedDays.Should().BeInRange(result.TotalEffort.MinDays, result.TotalEffort.MaxDays);
    }

    [Fact]
    public async Task EstimateAsync_WithComplexProject_ReturnsHigherEstimate()
    {
        // Arrange
        var simpleRequest = new EstimateRequest
        {
            ProjectDescription = "Simple API",
            TechStack = new List<string> { ".NET" }
        };

        var complexRequest = new EstimateRequest
        {
            ProjectDescription = "Build a microservices platform with authentication, payment integration, real-time features",
            RequiredFeatures = new List<string>
            {
                "authentication",
                "microservices",
                "payment integration",
                "real-time features"
            },
            TechStack = new List<string> { ".NET", "Docker", "RabbitMQ" },
            Constraints = new List<string> { "high availability", "security compliance" }
        };

        // Act
        var simpleResult = await _sut.EstimateAsync(simpleRequest);
        var complexResult = await _sut.EstimateAsync(complexRequest);

        // Assert
        complexResult.TotalEffort.RecommendedDays.Should().BeGreaterThan(simpleResult.TotalEffort.RecommendedDays);
    }

    [Fact]
    public async Task EstimateAsync_WithFamiliarTechStack_HasHigherConfidence()
    {
        // Arrange
        var familiarRequest = new EstimateRequest
        {
            ProjectDescription = "Build an API",
            TechStack = new List<string> { ".NET", "C#", "PostgreSQL" }
        };

        var unfamiliarRequest = new EstimateRequest
        {
            ProjectDescription = "Build an API",
            TechStack = new List<string> { "Ruby", "Rails", "MongoDB" }
        };

        // Act
        var familiarResult = await _sut.EstimateAsync(familiarRequest);
        var unfamiliarResult = await _sut.EstimateAsync(unfamiliarRequest);

        // Assert
        familiarResult.ConfidenceScore.Should().BeGreaterThanOrEqualTo(unfamiliarResult.ConfidenceScore);
    }

    [Fact]
    public async Task EstimateAsync_ReturnsPhases()
    {
        // Arrange
        var request = new EstimateRequest
        {
            ProjectDescription = "Build a complete application",
            DetailedBreakdown = true
        };

        // Act
        var result = await _sut.EstimateAsync(request);

        // Assert
        result.Phases.Should().NotBeEmpty();
        result.Phases.Should().Contain(p => p.PhaseName.Contains("Planning"));
        result.Phases.Should().Contain(p => p.PhaseName.Contains("Development"));
        result.Phases.Should().Contain(p => p.PhaseName.Contains("Testing"));
    }

    [Fact]
    public async Task EstimateAsync_IncludesAssumptionsAndRisks()
    {
        // Arrange
        var request = new EstimateRequest
        {
            ProjectDescription = "Build an application with third-party API integration",
            RequiredFeatures = new List<string> { "API integration" }
        };

        // Act
        var result = await _sut.EstimateAsync(request);

        // Assert
        result.Assumptions.Should().NotBeEmpty();
        result.Risks.Should().NotBeEmpty();
    }

    [Fact]
    public async Task EstimateAsync_IncludesNdaNote()
    {
        // Arrange
        var request = new EstimateRequest
        {
            ProjectDescription = "Any project"
        };

        // Act
        var result = await _sut.EstimateAsync(request);

        // Assert
        result.NdaNote.Should().NotBeEmpty();
        result.NdaNote.Should().Contain("NDA");
    }

    [Fact]
    public async Task EstimateAsync_WithConstraints_AdjustsEstimate()
    {
        // Arrange
        var baseRequest = new EstimateRequest
        {
            ProjectDescription = "Build an API"
        };

        var constrainedRequest = new EstimateRequest
        {
            ProjectDescription = "Build an API",
            Constraints = new List<string>
            {
                "high availability requirement",
                "security compliance required"
            }
        };

        // Act
        var baseResult = await _sut.EstimateAsync(baseRequest);
        var constrainedResult = await _sut.EstimateAsync(constrainedRequest);

        // Assert
        constrainedResult.TotalEffort.MaxDays.Should().BeGreaterThanOrEqualTo(baseResult.TotalEffort.MaxDays);
    }

    [Fact]
    public async Task EstimateAsync_ReturnsRecommendedTechnologies()
    {
        // Arrange
        var request = new EstimateRequest
        {
            ProjectDescription = "Build a backend API with database"
        };

        // Act
        var result = await _sut.EstimateAsync(request);

        // Assert
        result.RecommendedTechnologies.Should().NotBeEmpty();
        result.RecommendedTechnologies.Should().Contain(t => t.Contains(".NET"));
    }
}
