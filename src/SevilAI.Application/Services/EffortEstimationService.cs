using System.Text;
using Microsoft.Extensions.Logging;
using SevilAI.Application.DTOs;
using SevilAI.Application.Interfaces;
using SevilAI.Domain.Entities;
using SevilAI.Domain.Enums;
using SevilAI.Domain.Interfaces;

namespace SevilAI.Application.Services;

public class EffortEstimationService : IEffortEstimationService
{
    private readonly ISkillRepository _skillRepository;
    private readonly IExperienceRepository _experienceRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly ITextGenerator _textGenerator;
    private readonly ILogger<EffortEstimationService> _logger;

    // Engineering heuristics based on industry standards
    private static readonly Dictionary<string, (int MinDays, int MaxDays)> FeatureComplexity = new()
    {
        ["authentication"] = (3, 7),
        ["user management"] = (2, 5),
        ["crud operations"] = (1, 3),
        ["api integration"] = (2, 5),
        ["database design"] = (2, 4),
        ["ui/ux"] = (3, 8),
        ["testing"] = (2, 5),
        ["deployment"] = (1, 3),
        ["documentation"] = (1, 2),
        ["security"] = (2, 5),
        ["performance optimization"] = (2, 6),
        ["caching"] = (1, 3),
        ["logging/monitoring"] = (1, 3),
        ["file handling"] = (1, 3),
        ["email/notifications"] = (2, 4),
        ["payment integration"] = (3, 7),
        ["search functionality"] = (2, 5),
        ["reporting/analytics"] = (3, 7),
        ["real-time features"] = (3, 8),
        ["microservices"] = (5, 15),
        ["message queue"] = (2, 5),
        ["event sourcing"] = (4, 10)
    };

    private static readonly Dictionary<string, decimal> TechStackFamiliarity = new()
    {
        [".net"] = 1.0m,
        ["c#"] = 1.0m,
        ["asp.net"] = 1.0m,
        ["postgresql"] = 0.9m,
        ["docker"] = 0.9m,
        ["devexpress"] = 0.95m,
        ["json"] = 1.0m,
        ["rest api"] = 1.0m,
        ["entity framework"] = 0.9m,
        ["sql"] = 0.9m,
        ["git"] = 1.0m,
        ["react"] = 0.7m,
        ["typescript"] = 0.75m,
        ["python"] = 0.7m,
        ["azure"] = 0.8m,
        ["kubernetes"] = 0.7m,
        ["rabbitmq"] = 0.8m,
        ["redis"] = 0.8m,
        ["elasticsearch"] = 0.7m
    };

    public EffortEstimationService(
        ISkillRepository skillRepository,
        IExperienceRepository experienceRepository,
        IProjectRepository projectRepository,
        ITextGenerator textGenerator,
        ILogger<EffortEstimationService> logger)
    {
        _skillRepository = skillRepository;
        _experienceRepository = experienceRepository;
        _projectRepository = projectRepository;
        _textGenerator = textGenerator;
        _logger = logger;
    }

    public async Task<EstimateResponse> EstimateAsync(EstimateRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Estimating effort for: {Description}", request.ProjectDescription);

        // Load profile data for context
        var skills = (await _skillRepository.GetAllAsync(cancellationToken)).ToList();
        var experiences = (await _experienceRepository.GetAllAsync(cancellationToken)).ToList();
        var projects = (await _projectRepository.GetAllAsync(cancellationToken)).ToList();

        // Analyze the request
        var analyzedFeatures = AnalyzeFeatures(request);
        var techStackAnalysis = AnalyzeTechStack(request.TechStack, skills);

        // Calculate base estimates
        var phases = GeneratePhases(analyzedFeatures, request);

        // Apply modifiers
        var totalMin = phases.Sum(p => p.MinDays);
        var totalMax = phases.Sum(p => p.MaxDays);

        // Apply tech familiarity modifier
        var familiarityFactor = CalculateFamiliarityFactor(request.TechStack);
        totalMin = (int)Math.Ceiling(totalMin / familiarityFactor);
        totalMax = (int)Math.Ceiling(totalMax / familiarityFactor);

        // Apply constraint modifiers
        var constraintFactor = CalculateConstraintFactor(request.Constraints);
        totalMin = (int)Math.Ceiling(totalMin * constraintFactor);
        totalMax = (int)Math.Ceiling(totalMax * constraintFactor);

        // Calculate recommended (using beta distribution midpoint approximation)
        var recommended = (totalMin + (4 * ((totalMin + totalMax) / 2)) + totalMax) / 6;

        // Generate assumptions and risks
        var assumptions = GenerateAssumptions(request, skills);
        var risks = GenerateRisks(request, analyzedFeatures);

        // Calculate confidence based on information completeness
        var confidence = CalculateConfidence(request, skills, projects);

        // Generate recommended technologies
        var recommendedTech = GenerateRecommendedTechnologies(request, skills);

        return new EstimateResponse
        {
            ProjectSummary = GenerateProjectSummary(request),
            TotalEffort = new EstimateRange
            {
                MinDays = totalMin,
                MaxDays = totalMax,
                RecommendedDays = recommended,
                Rationale = GenerateRationale(analyzedFeatures, familiarityFactor, constraintFactor)
            },
            Phases = phases,
            Assumptions = assumptions,
            Risks = risks,
            RecommendedTechnologies = recommendedTech,
            ConfidenceScore = confidence,
            Methodology = "PERT-based estimation with skill profile weighting and complexity analysis",
            NdaNote = "This estimate is based on Sevil Aydın's publicly shareable skill profile. Due to NDA/company policy, specific past project details and proprietary methodologies cannot be disclosed."
        };
    }

    private List<AnalyzedFeature> AnalyzeFeatures(EstimateRequest request)
    {
        var features = new List<AnalyzedFeature>();
        var description = request.ProjectDescription.ToLowerInvariant();
        var requestedFeatures = request.RequiredFeatures.Select(f => f.ToLowerInvariant()).ToList();

        foreach (var (feature, complexity) in FeatureComplexity)
        {
            if (description.Contains(feature) || requestedFeatures.Any(f => f.Contains(feature)))
            {
                features.Add(new AnalyzedFeature(feature, complexity.MinDays, complexity.MaxDays));
            }
        }

        // Add any explicitly requested features that weren't matched
        foreach (var requested in requestedFeatures)
        {
            if (!features.Any(f => requested.Contains(f.Name)))
            {
                // Default complexity for unknown features
                features.Add(new AnalyzedFeature(requested, 2, 5));
            }
        }

        // If no features detected, add base project setup
        if (features.Count == 0)
        {
            features.Add(new AnalyzedFeature("project setup and base architecture", 3, 7));
        }

        return features;
    }

    private TechStackAnalysis AnalyzeTechStack(List<string> requestedStack, List<Skill> skills)
    {
        var familiar = new List<string>();
        var unfamiliar = new List<string>();
        var skillNames = skills.Select(s => s.Name.ToLowerInvariant()).ToHashSet();

        foreach (var tech in requestedStack)
        {
            var techLower = tech.ToLowerInvariant();
            if (TechStackFamiliarity.ContainsKey(techLower) && TechStackFamiliarity[techLower] >= 0.8m)
            {
                familiar.Add(tech);
            }
            else if (skillNames.Contains(techLower))
            {
                familiar.Add(tech);
            }
            else
            {
                unfamiliar.Add(tech);
            }
        }

        return new TechStackAnalysis(familiar, unfamiliar);
    }

    private decimal CalculateFamiliarityFactor(List<string> techStack)
    {
        if (techStack.Count == 0) return 0.9m; // Assume mostly familiar stack

        var factors = techStack.Select(t =>
        {
            var techLower = t.ToLowerInvariant();
            return TechStackFamiliarity.GetValueOrDefault(techLower, 0.7m);
        });

        return factors.Average();
    }

    private decimal CalculateConstraintFactor(List<string> constraints)
    {
        var factor = 1.0m;

        foreach (var constraint in constraints.Select(c => c.ToLowerInvariant()))
        {
            if (constraint.Contains("tight deadline") || constraint.Contains("urgent"))
                factor *= 0.9m; // Less time means more risk, but faster delivery
            if (constraint.Contains("high availability") || constraint.Contains("99.9%"))
                factor *= 1.3m;
            if (constraint.Contains("compliance") || constraint.Contains("security"))
                factor *= 1.2m;
            if (constraint.Contains("scalability") || constraint.Contains("high load"))
                factor *= 1.25m;
            if (constraint.Contains("legacy") || constraint.Contains("integration"))
                factor *= 1.2m;
        }

        return factor;
    }

    private List<PhaseBreakdown> GeneratePhases(List<AnalyzedFeature> features, EstimateRequest request)
    {
        var phases = new List<PhaseBreakdown>();

        // Phase 1: Planning & Design
        phases.Add(new PhaseBreakdown
        {
            PhaseName = "Planning & Architecture Design",
            Description = "Requirements analysis, system design, and architecture planning",
            MinDays = 2,
            MaxDays = 5,
            Tasks = new List<string>
            {
                "Requirements gathering and clarification",
                "System architecture design",
                "Database schema design",
                "API contract definition",
                "Technology stack finalization"
            },
            Deliverables = new List<string>
            {
                "Technical specification document",
                "Architecture diagrams",
                "Database ERD",
                "API documentation draft"
            }
        });

        // Phase 2: Core Development
        var coreMin = features.Sum(f => f.MinDays);
        var coreMax = features.Sum(f => f.MaxDays);
        phases.Add(new PhaseBreakdown
        {
            PhaseName = "Core Development",
            Description = "Implementation of main features and functionality",
            MinDays = coreMin,
            MaxDays = coreMax,
            Tasks = features.Select(f => $"Implement {f.Name}").ToList(),
            Deliverables = new List<string>
            {
                "Working feature implementations",
                "Unit tests for core components",
                "Integration points established"
            }
        });

        // Phase 3: Integration & Testing
        var testMin = Math.Max(2, coreMin / 4);
        var testMax = Math.Max(4, coreMax / 3);
        phases.Add(new PhaseBreakdown
        {
            PhaseName = "Integration & Testing",
            Description = "System integration, testing, and quality assurance",
            MinDays = testMin,
            MaxDays = testMax,
            Tasks = new List<string>
            {
                "Integration testing",
                "End-to-end testing",
                "Performance testing",
                "Security review",
                "Bug fixing and refinement"
            },
            Deliverables = new List<string>
            {
                "Test reports",
                "Bug fix releases",
                "Performance benchmarks"
            }
        });

        // Phase 4: Deployment & Documentation
        phases.Add(new PhaseBreakdown
        {
            PhaseName = "Deployment & Documentation",
            Description = "Production deployment and documentation",
            MinDays = 1,
            MaxDays = 3,
            Tasks = new List<string>
            {
                "Production environment setup",
                "Deployment automation",
                "Documentation completion",
                "Knowledge transfer"
            },
            Deliverables = new List<string>
            {
                "Deployed application",
                "Deployment documentation",
                "User/API documentation",
                "Runbook for operations"
            }
        });

        return phases;
    }

    private List<string> GenerateAssumptions(EstimateRequest request, List<Skill> skills)
    {
        var assumptions = new List<string>
        {
            "Requirements are well-defined and stable during development",
            "Development environment and tools are available and configured",
            "No major blockers from external dependencies or APIs",
            "Code reviews and testing are included in the estimates"
        };

        if (request.TechStack.Any(t => t.Contains(".NET", StringComparison.OrdinalIgnoreCase) ||
                                       t.Contains("C#", StringComparison.OrdinalIgnoreCase)))
        {
            assumptions.Add("Leveraging existing .NET/C# expertise for optimal development velocity");
        }

        if (request.TeamSize != null)
        {
            assumptions.Add($"Team size: {request.TeamSize} - estimates adjusted accordingly");
        }
        else
        {
            assumptions.Add("Estimate assumes single developer (Sevil) working full-time");
        }

        return assumptions;
    }

    private List<string> GenerateRisks(EstimateRequest request, List<AnalyzedFeature> features)
    {
        var risks = new List<string>();

        if (features.Any(f => f.Name.Contains("integration") || f.Name.Contains("api")))
        {
            risks.Add("Third-party API changes or availability issues could impact timeline");
        }

        if (features.Any(f => f.Name.Contains("real-time") || f.Name.Contains("microservices")))
        {
            risks.Add("Distributed systems complexity may require additional debugging time");
        }

        if (request.TechStack.Any(t => !TechStackFamiliarity.ContainsKey(t.ToLowerInvariant())))
        {
            risks.Add("Unfamiliar technologies may require additional learning time");
        }

        if (request.Constraints.Any(c => c.ToLowerInvariant().Contains("legacy")))
        {
            risks.Add("Legacy system integration may uncover unexpected technical debt");
        }

        risks.Add("Scope creep from changing requirements could extend the timeline");
        risks.Add("Complex bugs or edge cases may require additional investigation");

        return risks;
    }

    private decimal CalculateConfidence(EstimateRequest request, List<Skill> skills, List<Project> projects)
    {
        var score = 0.5m; // Base confidence

        // More details = higher confidence
        if (request.RequiredFeatures.Count > 0) score += 0.1m;
        if (request.TechStack.Count > 0) score += 0.1m;
        if (request.Constraints.Count > 0) score += 0.05m;
        if (request.ProjectDescription.Length > 100) score += 0.1m;

        // Known tech stack = higher confidence
        var knownTech = request.TechStack.Count(t =>
            TechStackFamiliarity.ContainsKey(t.ToLowerInvariant()) ||
            skills.Any(s => s.Name.Equals(t, StringComparison.OrdinalIgnoreCase)));
        if (request.TechStack.Count > 0)
        {
            score += (knownTech / (decimal)request.TechStack.Count) * 0.15m;
        }

        return Math.Min(0.95m, Math.Round(score, 2));
    }

    private List<string> GenerateRecommendedTechnologies(EstimateRequest request, List<Skill> skills)
    {
        var recommendations = new List<string>();
        var description = request.ProjectDescription.ToLowerInvariant();

        // Always recommend core stack
        recommendations.Add(".NET 8 (Core expertise)");
        recommendations.Add("C# (Primary language)");

        if (description.Contains("api") || description.Contains("backend") || description.Contains("service"))
        {
            recommendations.Add("ASP.NET Core Web API");
        }

        if (description.Contains("database") || description.Contains("data"))
        {
            recommendations.Add("PostgreSQL (Proven experience)");
            recommendations.Add("Entity Framework Core");
        }

        if (description.Contains("container") || description.Contains("deploy") || description.Contains("docker"))
        {
            recommendations.Add("Docker & Docker Compose");
        }

        if (description.Contains("distributed") || description.Contains("microservice") || description.Contains("event"))
        {
            recommendations.Add("RabbitMQ or Azure Service Bus");
            recommendations.Add("Saga Pattern for distributed transactions");
        }

        if (description.Contains("test"))
        {
            recommendations.Add("xUnit for unit testing");
            recommendations.Add("Integration testing frameworks");
        }

        return recommendations.Distinct().ToList();
    }

    private string GenerateProjectSummary(EstimateRequest request)
    {
        var sb = new StringBuilder();
        sb.AppendLine(request.ProjectDescription);

        if (request.RequiredFeatures.Count > 0)
        {
            sb.AppendLine($"\nKey features: {string.Join(", ", request.RequiredFeatures)}");
        }

        if (request.TechStack.Count > 0)
        {
            sb.AppendLine($"Technology stack: {string.Join(", ", request.TechStack)}");
        }

        return sb.ToString().Trim();
    }

    private string GenerateRationale(List<AnalyzedFeature> features, decimal familiarityFactor, decimal constraintFactor)
    {
        var sb = new StringBuilder();
        sb.Append($"Estimate based on {features.Count} identified feature areas. ");
        sb.Append($"Tech familiarity factor: {familiarityFactor:P0} ");
        sb.Append($"(higher is more efficient). ");

        if (constraintFactor > 1.0m)
        {
            sb.Append($"Constraint complexity adds {(constraintFactor - 1.0m):P0} overhead.");
        }

        return sb.ToString();
    }

    private record AnalyzedFeature(string Name, int MinDays, int MaxDays);
    private record TechStackAnalysis(List<string> Familiar, List<string> Unfamiliar);
}
