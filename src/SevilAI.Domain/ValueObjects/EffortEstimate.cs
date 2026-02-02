namespace SevilAI.Domain.ValueObjects;

public record EffortEstimate(
    string ProjectDescription,
    int MinDays,
    int MaxDays,
    int RecommendedDays,
    List<PhaseEstimate> Phases,
    List<string> Assumptions,
    List<string> Risks,
    decimal ConfidenceScore
);

public record PhaseEstimate(
    string PhaseName,
    string Description,
    int MinDays,
    int MaxDays,
    List<string> Tasks
);
