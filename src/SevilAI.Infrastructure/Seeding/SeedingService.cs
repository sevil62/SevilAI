using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SevilAI.Application.DTOs;
using SevilAI.Application.Interfaces;
using SevilAI.Domain.Entities;
using SevilAI.Domain.Enums;
using SevilAI.Domain.Interfaces;
using SevilAI.Infrastructure.Persistence;

namespace SevilAI.Infrastructure.Seeding;

public class SeedingService : ISeedingService
{
    private readonly SevilAIDbContext _context;
    private readonly IDocumentRepository _documentRepository;
    private readonly IChunkRepository _chunkRepository;
    private readonly IEmbeddingRepository _embeddingRepository;
    private readonly ISkillRepository _skillRepository;
    private readonly IExperienceRepository _experienceRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IChunkingService _chunkingService;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<SeedingService> _logger;

    public SeedingService(
        SevilAIDbContext context,
        IDocumentRepository documentRepository,
        IChunkRepository chunkRepository,
        IEmbeddingRepository embeddingRepository,
        ISkillRepository skillRepository,
        IExperienceRepository experienceRepository,
        IProjectRepository projectRepository,
        IChunkingService chunkingService,
        IEmbeddingService embeddingService,
        ILogger<SeedingService> logger)
    {
        _context = context;
        _documentRepository = documentRepository;
        _chunkRepository = chunkRepository;
        _embeddingRepository = embeddingRepository;
        _skillRepository = skillRepository;
        _experienceRepository = experienceRepository;
        _projectRepository = projectRepository;
        _chunkingService = chunkingService;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    public async Task<SeedResponse> SeedFromJsonAsync(string? jsonData, bool clearExisting, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = new SeedResponse();

        try
        {
            if (string.IsNullOrWhiteSpace(jsonData))
            {
                response.Errors.Add("JSON data is empty or null");
                return response;
            }

            if (clearExisting)
            {
                await ClearExistingDataAsync(cancellationToken);
            }

            var seedData = JsonSerializer.Deserialize<KnowledgeBaseSeedData>(jsonData, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (seedData == null)
            {
                response.Errors.Add("Failed to deserialize JSON data");
                return response;
            }

            await ProcessSeedDataAsync(seedData, response, cancellationToken);

            response.Success = response.Errors.Count == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding from JSON");
            response.Errors.Add($"Seeding error: {ex.Message}");
        }

        stopwatch.Stop();
        response.DurationMs = (int)stopwatch.ElapsedMilliseconds;

        return response;
    }

    public async Task<SeedResponse> SeedFromEmbeddedResourceAsync(bool clearExisting, CancellationToken cancellationToken = default)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var response = new SeedResponse();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (clearExisting)
            {
                await ClearExistingDataAsync(cancellationToken);
            }

            // 1. Load and process knowledge base
            var knowledgeBaseResource = "SevilAI.Infrastructure.Seeding.SeedData.sevil_knowledge_base.json";
            using var kbStream = assembly.GetManifestResourceStream(knowledgeBaseResource);
            if (kbStream != null)
            {
                using var kbReader = new StreamReader(kbStream);
                var kbJson = await kbReader.ReadToEndAsync(cancellationToken);
                var kbData = JsonSerializer.Deserialize<KnowledgeBaseSeedData>(kbJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (kbData != null)
                {
                    await ProcessSeedDataAsync(kbData, response, cancellationToken);
                    _logger.LogInformation("Knowledge base seeded successfully");
                }
            }
            else
            {
                _logger.LogWarning("Knowledge base resource not found: {Resource}", knowledgeBaseResource);
            }

            // 2. Load and process training dataset (Q&A scenarios)
            var trainingResource = "SevilAI.Infrastructure.Seeding.SeedData.sevil_training_dataset.json";
            using var trainStream = assembly.GetManifestResourceStream(trainingResource);
            if (trainStream != null)
            {
                using var trainReader = new StreamReader(trainStream);
                var trainJson = await trainReader.ReadToEndAsync(cancellationToken);
                await ProcessTrainingDatasetAsync(trainJson, response, cancellationToken);
                _logger.LogInformation("Training dataset seeded successfully");
            }
            else
            {
                _logger.LogWarning("Training dataset resource not found: {Resource}", trainingResource);
            }

            response.Success = response.Errors.Count == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding from embedded resources");
            response.Errors.Add($"Seeding error: {ex.Message}");
        }

        stopwatch.Stop();
        response.DurationMs = (int)stopwatch.ElapsedMilliseconds;

        return response;
    }

    private async Task ProcessTrainingDatasetAsync(
        string jsonData,
        SeedResponse response,
        CancellationToken cancellationToken)
    {
        var trainingData = JsonSerializer.Deserialize<TrainingDataset>(jsonData, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (trainingData?.Scenarios == null) return;

        // Process profile as document
        if (trainingData.Profile != null)
        {
            var profileContent = BuildProfileContent(trainingData.Profile);
            var profileDoc = await CreateDocumentWithChunksAsync(
                "Sevil Aydın - Complete Profile",
                "profile",
                profileContent,
                cancellationToken);

            response.DocumentsCreated++;
            response.ChunksCreated += profileDoc.Chunks.Count;
            response.EmbeddingsCreated += profileDoc.Chunks.Count;
        }

        // Process Q&A scenarios by category
        await ProcessScenarioCategoryAsync(trainingData.Scenarios.IdentityTr, "identity", "tr", response, cancellationToken);
        await ProcessScenarioCategoryAsync(trainingData.Scenarios.CareerTr, "career", "tr", response, cancellationToken);
        await ProcessScenarioCategoryAsync(trainingData.Scenarios.ProjectsTr, "projects", "tr", response, cancellationToken);
        await ProcessScenarioCategoryAsync(trainingData.Scenarios.TechnicalTr, "technical", "tr", response, cancellationToken);
        await ProcessScenarioCategoryAsync(trainingData.Scenarios.TeamworkTr, "teamwork", "tr", response, cancellationToken);
        await ProcessScenarioCategoryAsync(trainingData.Scenarios.PersonalityTr, "personality", "tr", response, cancellationToken);
        await ProcessScenarioCategoryAsync(trainingData.Scenarios.GoalsTr, "goals", "tr", response, cancellationToken);
        await ProcessScenarioCategoryAsync(trainingData.Scenarios.EdgeCasesTr, "edge_cases", "tr", response, cancellationToken);

        // English scenarios
        await ProcessScenarioCategoryAsync(trainingData.Scenarios.IdentityEn, "identity", "en", response, cancellationToken);
        await ProcessScenarioCategoryAsync(trainingData.Scenarios.CareerEn, "career", "en", response, cancellationToken);
        await ProcessScenarioCategoryAsync(trainingData.Scenarios.ProjectsEn, "projects", "en", response, cancellationToken);
        await ProcessScenarioCategoryAsync(trainingData.Scenarios.TechnicalEn, "technical", "en", response, cancellationToken);
        await ProcessScenarioCategoryAsync(trainingData.Scenarios.TeamworkEn, "teamwork", "en", response, cancellationToken);
        await ProcessScenarioCategoryAsync(trainingData.Scenarios.PersonalityEn, "personality", "en", response, cancellationToken);
        await ProcessScenarioCategoryAsync(trainingData.Scenarios.EdgeCasesEn, "edge_cases", "en", response, cancellationToken);
    }

    private string BuildProfileContent(ProfileData profile)
    {
        var sb = new StringBuilder();

        if (profile.Identity != null)
        {
            sb.AppendLine("## Identity");
            sb.AppendLine($"Full Name: {profile.Identity.FullName}");
            sb.AppendLine($"Title: {profile.Identity.Title}");
            sb.AppendLine($"Location: {profile.Identity.Location}");
            sb.AppendLine($"Current Company: {profile.Identity.CurrentCompany}");
            sb.AppendLine($"Years in Software: {profile.Identity.YearsInSoftware}");
            sb.AppendLine($"Total Engineering Experience: {profile.Identity.TotalEngineeringExperience}");
            sb.AppendLine();
        }

        if (profile.Personality != null)
        {
            sb.AppendLine("## Personality & Communication Style");
            sb.AppendLine($"Communication Style: {profile.Personality.CommunicationStyle}");

            if (profile.Personality.CoreTraits != null)
            {
                sb.AppendLine("\nCore Traits:");
                foreach (var trait in profile.Personality.CoreTraits)
                    sb.AppendLine($"- {trait}");
            }

            if (profile.Personality.Values != null)
            {
                sb.AppendLine("\nValues:");
                foreach (var value in profile.Personality.Values)
                    sb.AppendLine($"- {value}");
            }

            if (profile.Personality.Motivations != null)
            {
                sb.AppendLine("\nMotivations:");
                foreach (var motivation in profile.Personality.Motivations)
                    sb.AppendLine($"- {motivation}");
            }

            if (profile.Personality.Hobbies != null)
            {
                sb.AppendLine("\nHobbies:");
                foreach (var hobby in profile.Personality.Hobbies)
                    sb.AppendLine($"- {hobby}");
            }
            sb.AppendLine();
        }

        if (profile.WorkStyle != null)
        {
            sb.AppendLine("## Work Style");
            sb.AppendLine($"Efficiency: {profile.WorkStyle.Efficiency}");
            sb.AppendLine($"Approach: {profile.WorkStyle.Approach}");
            sb.AppendLine($"Ownership: {profile.WorkStyle.Ownership}");

            if (profile.WorkStyle.Strengths != null)
            {
                sb.AppendLine("\nStrengths:");
                foreach (var strength in profile.WorkStyle.Strengths)
                    sb.AppendLine($"- {strength}");
            }
        }

        return sb.ToString();
    }

    private async Task ProcessScenarioCategoryAsync(
        List<QAScenario>? scenarios,
        string category,
        string language,
        SeedResponse response,
        CancellationToken cancellationToken)
    {
        if (scenarios == null || scenarios.Count == 0) return;

        // Group scenarios into batches for chunking
        var batchSize = 5;
        var batches = scenarios
            .Select((s, i) => new { Scenario = s, Index = i })
            .GroupBy(x => x.Index / batchSize)
            .ToList();

        foreach (var batch in batches)
        {
            var content = new StringBuilder();
            content.AppendLine($"## {category.ToUpperInvariant()} Q&A ({language.ToUpperInvariant()})\n");

            foreach (var item in batch)
            {
                var scenario = item.Scenario;
                content.AppendLine($"**Q: {scenario.Question}**");
                content.AppendLine($"A: {scenario.Answer}");
                content.AppendLine();
            }

            var title = $"Q&A - {category} ({language}) - Batch {batch.Key + 1}";
            var document = await CreateDocumentWithChunksAsync(
                title,
                $"qa_{category}_{language}",
                content.ToString(),
                cancellationToken);

            response.DocumentsCreated++;
            response.ChunksCreated += document.Chunks.Count;
            response.EmbeddingsCreated += document.Chunks.Count;
        }
    }

    private async Task ClearExistingDataAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Clearing existing data...");

        // Delete in correct order due to foreign keys
        _context.Embeddings.RemoveRange(_context.Embeddings);
        _context.Chunks.RemoveRange(_context.Chunks);
        _context.Documents.RemoveRange(_context.Documents);
        _context.Skills.RemoveRange(_context.Skills);
        _context.Experiences.RemoveRange(_context.Experiences);
        _context.Projects.RemoveRange(_context.Projects);

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task ProcessSeedDataAsync(
        KnowledgeBaseSeedData seedData,
        SeedResponse response,
        CancellationToken cancellationToken)
    {
        // Process person/CV data
        if (seedData.Person != null)
        {
            await ProcessPersonDataAsync(seedData.Person, response, cancellationToken);
        }

        // Process character/traits
        if (seedData.Character != null)
        {
            await ProcessCharacterDataAsync(seedData.Character, response, cancellationToken);
        }

        // Process career journey
        if (seedData.CareerJourney != null)
        {
            foreach (var career in seedData.CareerJourney)
            {
                await ProcessCareerEntryAsync(career, response, cancellationToken);
            }
        }

        // Process enterprise project
        if (seedData.EnterpriseProject != null)
        {
            await ProcessProjectAsync(seedData.EnterpriseProject, ProjectType.Enterprise, response, cancellationToken);
        }

        // Process personal projects
        if (seedData.PersonalProjects != null)
        {
            foreach (var project in seedData.PersonalProjects)
            {
                await ProcessProjectAsync(project, ProjectType.Personal, response, cancellationToken);
            }
        }

        // Process effort profile
        if (seedData.EffortProfile != null)
        {
            await ProcessEffortProfileAsync(seedData.EffortProfile, response, cancellationToken);
        }
    }

    private async Task ProcessPersonDataAsync(
        PersonData person,
        SeedResponse response,
        CancellationToken cancellationToken)
    {
        // Create main CV document
        var cvContent = new StringBuilder();
        cvContent.AppendLine($"Name: {person.Name}");
        cvContent.AppendLine($"Title: {person.Title}");
        cvContent.AppendLine($"Location: {person.Location}");

        if (person.Focus != null)
        {
            cvContent.AppendLine($"\nCore Focus Areas: {string.Join(", ", person.Focus)}");
        }

        if (person.CareerGoal != null)
        {
            cvContent.AppendLine($"\nCareer Summary: {person.CareerGoal.Summary}");
            if (person.CareerGoal.LongTerm != null)
            {
                cvContent.AppendLine("\nLong-term Goals:");
                foreach (var goal in person.CareerGoal.LongTerm)
                {
                    cvContent.AppendLine($"- {goal}");
                }
            }
        }

        var document = await CreateDocumentWithChunksAsync(
            "Sevil Aydın - CV Summary",
            "cv",
            cvContent.ToString(),
            cancellationToken);

        response.DocumentsCreated++;
        response.ChunksCreated += document.Chunks.Count;
        response.EmbeddingsCreated += document.Chunks.Count;

        // Create skills from focus areas
        if (person.Focus != null)
        {
            foreach (var skill in person.Focus)
            {
                var category = DetermineSkillCategory(skill);
                await _skillRepository.AddAsync(new Skill
                {
                    Name = skill,
                    Category = category,
                    ProficiencyLevel = ProficiencyLevel.Advanced,
                    Description = $"Core focus area: {skill}"
                }, cancellationToken);
                response.SkillsCreated++;
            }
        }
    }

    private async Task ProcessCharacterDataAsync(
        CharacterData character,
        SeedResponse response,
        CancellationToken cancellationToken)
    {
        var content = new StringBuilder();
        content.AppendLine("## Professional Character & Work Style\n");

        if (!string.IsNullOrEmpty(character.WorkEthic))
        {
            content.AppendLine($"Work Ethic: {character.WorkEthic}");
        }

        if (character.Traits != null)
        {
            content.AppendLine("\nKey Traits:");
            foreach (var trait in character.Traits)
            {
                content.AppendLine($"- {trait}");
            }
        }

        if (character.TeamStyle != null)
        {
            content.AppendLine("\nTeam Collaboration Style:");
            foreach (var style in character.TeamStyle)
            {
                content.AppendLine($"- {style}");
            }
        }

        var document = await CreateDocumentWithChunksAsync(
            "Professional Character & Work Style",
            "character",
            content.ToString(),
            cancellationToken);

        response.DocumentsCreated++;
        response.ChunksCreated += document.Chunks.Count;
        response.EmbeddingsCreated += document.Chunks.Count;

        // Create soft skills
        if (character.Traits != null)
        {
            foreach (var trait in character.Traits)
            {
                await _skillRepository.AddAsync(new Skill
                {
                    Name = trait,
                    Category = SkillCategory.SoftSkill,
                    ProficiencyLevel = ProficiencyLevel.Advanced
                }, cancellationToken);
                response.SkillsCreated++;
            }
        }
    }

    private async Task ProcessCareerEntryAsync(
        CareerEntry career,
        SeedResponse response,
        CancellationToken cancellationToken)
    {
        var content = new StringBuilder();

        if (!string.IsNullOrEmpty(career.Company))
        {
            content.AppendLine($"## {career.Company}");
        }
        else if (!string.IsNullOrEmpty(career.Milestone))
        {
            content.AppendLine($"## {career.Milestone}");
        }

        content.AppendLine($"Period: {career.Period}");

        if (!string.IsNullOrEmpty(career.Role))
        {
            content.AppendLine($"Role: {career.Role}");
        }

        if (!string.IsNullOrEmpty(career.Team))
        {
            content.AppendLine($"Team: {career.Team}");
        }

        if (!string.IsNullOrEmpty(career.Responsibility))
        {
            content.AppendLine($"Responsibility: {career.Responsibility}");
        }

        if (career.Impact != null)
        {
            content.AppendLine("\nImpact:");
            foreach (var impact in career.Impact)
            {
                content.AppendLine($"- {impact}");
            }
        }

        if (career.Contribution != null)
        {
            content.AppendLine("\nContributions:");
            foreach (var contribution in career.Contribution)
            {
                content.AppendLine($"- {contribution}");
            }
        }

        if (career.Details != null)
        {
            content.AppendLine("\nDetails:");
            foreach (var detail in career.Details)
            {
                content.AppendLine($"- {detail}");
            }
        }

        if (!string.IsNullOrEmpty(career.TurningPoint))
        {
            content.AppendLine($"\nTurning Point: {career.TurningPoint}");
        }

        if (!string.IsNullOrEmpty(career.NdaNote))
        {
            content.AppendLine($"\nNote: {career.NdaNote}");
        }

        var title = career.Company ?? career.Milestone ?? $"Career Entry - {career.Period}";
        var document = await CreateDocumentWithChunksAsync(
            title,
            "experience",
            content.ToString(),
            cancellationToken);

        response.DocumentsCreated++;
        response.ChunksCreated += document.Chunks.Count;
        response.EmbeddingsCreated += document.Chunks.Count;

        // Create experience entity
        var experience = new Experience
        {
            Company = career.Company ?? "Self-Development",
            Role = career.Role ?? career.Milestone ?? "N/A",
            Description = content.ToString(),
            IsCurrent = career.Period?.Contains("Present") ?? false,
            Achievements = career.Impact?.Concat(career.Contribution ?? Enumerable.Empty<string>()).ToList() ?? new List<string>(),
            Technologies = career.Tech ?? new List<string>(),
            IsConfidential = career.NdaNote != null,
            NdaNote = career.NdaNote
        };

        // Parse period
        if (!string.IsNullOrEmpty(career.Period))
        {
            var parts = career.Period.Split('-', StringSplitOptions.TrimEntries);
            if (parts.Length >= 1 && int.TryParse(parts[0], out var startYear))
            {
                experience.PeriodStart = new DateOnly(startYear, 1, 1);
            }
            if (parts.Length >= 2 && int.TryParse(parts[1], out var endYear))
            {
                experience.PeriodEnd = new DateOnly(endYear, 12, 31);
            }
        }

        await _experienceRepository.AddAsync(experience, cancellationToken);
        response.ExperiencesCreated++;
    }

    private async Task ProcessProjectAsync(
        ProjectData project,
        ProjectType projectType,
        SeedResponse response,
        CancellationToken cancellationToken)
    {
        var content = new StringBuilder();
        content.AppendLine($"## Project: {project.Name}");
        content.AppendLine($"Type: {project.Type ?? projectType.ToString()}");

        if (project.Purpose != null)
        {
            content.AppendLine("\nPurpose:");
            foreach (var purpose in project.Purpose)
            {
                content.AppendLine($"- {purpose}");
            }
        }

        if (project.Capabilities != null)
        {
            content.AppendLine("\nCapabilities:");
            foreach (var cap in project.Capabilities)
            {
                content.AppendLine($"- {cap}");
            }
        }

        if (project.Features != null)
        {
            content.AppendLine("\nFeatures:");
            foreach (var feature in project.Features)
            {
                content.AppendLine($"- {feature}");
            }
        }

        if (!string.IsNullOrEmpty(project.Description))
        {
            content.AppendLine($"\nDescription: {project.Description}");
        }

        if (project.Services != null)
        {
            content.AppendLine("\nServices/Components:");
            foreach (var service in project.Services)
            {
                content.AppendLine($"- {service.Name}: {service.Purpose}");
                if (service.Features != null)
                {
                    foreach (var feature in service.Features)
                    {
                        content.AppendLine($"  * {feature}");
                    }
                }
            }
        }

        if (project.Stack != null)
        {
            content.AppendLine($"\nTechnology Stack: {string.Join(", ", project.Stack)}");
        }

        if (!string.IsNullOrEmpty(project.Architecture))
        {
            content.AppendLine($"\nArchitecture: {project.Architecture}");
        }

        if (project.Patterns != null)
        {
            content.AppendLine("\nDesign Patterns:");
            foreach (var pattern in project.Patterns)
            {
                content.AppendLine($"- {pattern}");
            }
        }

        if (project.Learnings != null)
        {
            content.AppendLine("\nKey Learnings:");
            foreach (var learning in project.Learnings)
            {
                content.AppendLine($"- {learning}");
            }
        }

        if (project.Confidential == true)
        {
            content.AppendLine("\nNote: Due to NDA/company policy, specific source code and client details cannot be shared.");
        }

        var document = await CreateDocumentWithChunksAsync(
            project.Name,
            "project",
            content.ToString(),
            cancellationToken);

        response.DocumentsCreated++;
        response.ChunksCreated += document.Chunks.Count;
        response.EmbeddingsCreated += document.Chunks.Count;

        // Create project entity
        var projectEntity = new Project
        {
            Name = project.Name,
            ProjectType = projectType,
            Description = content.ToString(),
            Technologies = project.Stack ?? new List<string>(),
            Features = project.Features ?? project.Capabilities ?? new List<string>(),
            ArchitectureNotes = project.Architecture,
            IsConfidential = project.Confidential ?? false,
            NdaNote = project.Confidential == true
                ? "Due to NDA/company policy, specific source code and client details cannot be shared."
                : null
        };

        await _projectRepository.AddAsync(projectEntity, cancellationToken);
        response.ProjectsCreated++;
    }

    private async Task ProcessEffortProfileAsync(
        EffortProfileData profile,
        SeedResponse response,
        CancellationToken cancellationToken)
    {
        var content = new StringBuilder();
        content.AppendLine("## Effort & Productivity Profile\n");

        if (!string.IsNullOrEmpty(profile.WorkRate))
        {
            content.AppendLine($"Work Rate: {profile.WorkRate}");
        }

        if (!string.IsNullOrEmpty(profile.Adaptability))
        {
            content.AppendLine($"Adaptability: {profile.Adaptability}");
        }

        if (!string.IsNullOrEmpty(profile.EstimationStyle))
        {
            content.AppendLine($"Estimation Style: {profile.EstimationStyle}");
        }

        var document = await CreateDocumentWithChunksAsync(
            "Effort & Productivity Profile",
            "profile",
            content.ToString(),
            cancellationToken);

        response.DocumentsCreated++;
        response.ChunksCreated += document.Chunks.Count;
        response.EmbeddingsCreated += document.Chunks.Count;
    }

    private async Task<Document> CreateDocumentWithChunksAsync(
        string title,
        string sourceType,
        string content,
        CancellationToken cancellationToken)
    {
        var document = new Document
        {
            Title = title,
            SourceType = sourceType,
            Content = content
        };

        await _documentRepository.AddAsync(document, cancellationToken);

        // Create chunks
        var textChunks = _chunkingService.ChunkText(content).ToList();
        var chunks = new List<Chunk>();

        foreach (var textChunk in textChunks)
        {
            var chunk = new Chunk
            {
                DocumentId = document.Id,
                ChunkIndex = textChunk.Index,
                Content = textChunk.Content,
                TokenCount = textChunk.TokenCount
            };
            chunks.Add(chunk);
        }

        await _chunkRepository.AddRangeAsync(chunks, cancellationToken);
        document.Chunks = chunks;

        // Generate embeddings
        var embeddings = new List<Embedding>();
        var embeddingVectors = await _embeddingService.GenerateEmbeddingsAsync(
            chunks.Select(c => c.Content),
            cancellationToken);

        var vectorList = embeddingVectors.ToList();
        for (int i = 0; i < chunks.Count; i++)
        {
            var embedding = new Embedding
            {
                ChunkId = chunks[i].Id,
                Vector = vectorList[i],
                ModelName = _embeddingService.ModelName
            };
            embeddings.Add(embedding);
        }

        await _embeddingRepository.AddRangeAsync(embeddings, cancellationToken);

        return document;
    }

    private SkillCategory DetermineSkillCategory(string skill)
    {
        var skillLower = skill.ToLowerInvariant();

        if (skillLower.Contains(".net") || skillLower.Contains("c#"))
            return SkillCategory.Language;

        if (skillLower.Contains("docker") || skillLower.Contains("git") || skillLower.Contains("test"))
            return SkillCategory.Tool;

        if (skillLower.Contains("postgresql") || skillLower.Contains("sql") || skillLower.Contains("database"))
            return SkillCategory.Database;

        if (skillLower.Contains("architecture") || skillLower.Contains("design") || skillLower.Contains("distributed"))
            return SkillCategory.Architecture;

        if (skillLower.Contains("integration") || skillLower.Contains("configuration") || skillLower.Contains("system"))
            return SkillCategory.Domain;

        return SkillCategory.Framework;
    }
}

// Seed data model classes
public class KnowledgeBaseSeedData
{
    public PersonData? Person { get; set; }
    public CharacterData? Character { get; set; }
    public List<CareerEntry>? CareerJourney { get; set; }
    public ProjectData? EnterpriseProject { get; set; }
    public List<ProjectData>? PersonalProjects { get; set; }
    public EffortProfileData? EffortProfile { get; set; }
}

public class PersonData
{
    public string Name { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Location { get; set; }
    public List<string>? Focus { get; set; }
    public CareerGoalData? CareerGoal { get; set; }
}

public class CareerGoalData
{
    public string? Summary { get; set; }
    public List<string>? LongTerm { get; set; }
}

public class CharacterData
{
    public string? WorkEthic { get; set; }
    public List<string>? Traits { get; set; }
    public List<string>? TeamStyle { get; set; }
}

public class CareerEntry
{
    public string? Company { get; set; }
    public string? Period { get; set; }
    public string? Role { get; set; }
    public string? Team { get; set; }
    public string? Responsibility { get; set; }
    public string? Milestone { get; set; }
    public string? TurningPoint { get; set; }
    public string? NdaNote { get; set; }
    public List<string>? Impact { get; set; }
    public List<string>? Contribution { get; set; }
    public List<string>? Details { get; set; }
    public List<string>? Tech { get; set; }
}

public class ProjectData
{
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? Description { get; set; }
    public bool? Confidential { get; set; }
    public string? Architecture { get; set; }
    public List<string>? Purpose { get; set; }
    public List<string>? Features { get; set; }
    public List<string>? Capabilities { get; set; }
    public List<ServiceData>? Services { get; set; }
    public List<string>? Stack { get; set; }
    public List<string>? Patterns { get; set; }
    public List<string>? Learnings { get; set; }
}

public class ServiceData
{
    public string? Name { get; set; }
    public string? Purpose { get; set; }
    public List<string>? Features { get; set; }
}

public class EffortProfileData
{
    public string? WorkRate { get; set; }
    public string? Adaptability { get; set; }
    public string? EstimationStyle { get; set; }
}

// Training dataset model classes
public class TrainingDataset
{
    public TrainingMetadata? Metadata { get; set; }
    public SystemPromptData? SystemPrompt { get; set; }
    public ProfileData? Profile { get; set; }
    public ScenariosData? Scenarios { get; set; }
}

public class TrainingMetadata
{
    public string? Version { get; set; }
    public string? Created { get; set; }
    public string? Description { get; set; }
    public int TotalScenarios { get; set; }
    public List<string>? Languages { get; set; }
    public List<string>? Categories { get; set; }
}

public class SystemPromptData
{
    public string? Tr { get; set; }
    public string? En { get; set; }
}

public class ProfileData
{
    public IdentityData? Identity { get; set; }
    public PersonalityData? Personality { get; set; }
    public WorkStyleData? WorkStyle { get; set; }
}

public class IdentityData
{
    public string? FullName { get; set; }
    public string? Title { get; set; }
    public string? Location { get; set; }
    public string? CurrentCompany { get; set; }
    public string? YearsInSoftware { get; set; }
    public string? TotalEngineeringExperience { get; set; }
}

public class PersonalityData
{
    public string? CommunicationStyle { get; set; }
    public List<string>? CoreTraits { get; set; }
    public List<string>? Values { get; set; }
    public List<string>? Motivations { get; set; }
    public List<string>? Hobbies { get; set; }
}

public class WorkStyleData
{
    public string? Efficiency { get; set; }
    public string? Approach { get; set; }
    public string? Ownership { get; set; }
    public List<string>? Strengths { get; set; }
}

public class ScenariosData
{
    [JsonPropertyName("identity_tr")]
    public List<QAScenario>? IdentityTr { get; set; }

    [JsonPropertyName("career_tr")]
    public List<QAScenario>? CareerTr { get; set; }

    [JsonPropertyName("projects_tr")]
    public List<QAScenario>? ProjectsTr { get; set; }

    [JsonPropertyName("technical_tr")]
    public List<QAScenario>? TechnicalTr { get; set; }

    [JsonPropertyName("teamwork_tr")]
    public List<QAScenario>? TeamworkTr { get; set; }

    [JsonPropertyName("personality_tr")]
    public List<QAScenario>? PersonalityTr { get; set; }

    [JsonPropertyName("goals_tr")]
    public List<QAScenario>? GoalsTr { get; set; }

    [JsonPropertyName("edge_cases_tr")]
    public List<QAScenario>? EdgeCasesTr { get; set; }

    [JsonPropertyName("identity_en")]
    public List<QAScenario>? IdentityEn { get; set; }

    [JsonPropertyName("career_en")]
    public List<QAScenario>? CareerEn { get; set; }

    [JsonPropertyName("projects_en")]
    public List<QAScenario>? ProjectsEn { get; set; }

    [JsonPropertyName("technical_en")]
    public List<QAScenario>? TechnicalEn { get; set; }

    [JsonPropertyName("teamwork_en")]
    public List<QAScenario>? TeamworkEn { get; set; }

    [JsonPropertyName("personality_en")]
    public List<QAScenario>? PersonalityEn { get; set; }

    [JsonPropertyName("edge_cases_en")]
    public List<QAScenario>? EdgeCasesEn { get; set; }
}

public class QAScenario
{
    public string? Id { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public string? Context { get; set; }
    public List<string>? Tags { get; set; }
}
