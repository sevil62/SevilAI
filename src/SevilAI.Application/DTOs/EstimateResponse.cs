namespace SevilAI.Application.DTOs;

public class EstimateResponse
{
    public string ProjectSummary { get; set; } = string.Empty;
    public EstimateRange TotalEffort { get; set; } = new();
    public List<PhaseBreakdown> Phases { get; set; } = new();
    public List<string> Assumptions { get; set; } = new();
    public List<string> Risks { get; set; } = new();
    public List<string> RecommendedTechnologies { get; set; } = new();
    public decimal ConfidenceScore { get; set; }
    public string Methodology { get; set; } = string.Empty;
    public string NdaNote { get; set; } = string.Empty;
}

public class EstimateRange
{
    public int MinDays { get; set; }
    public int MaxDays { get; set; }
    public int RecommendedDays { get; set; }
    public string Rationale { get; set; } = string.Empty;
}

public class PhaseBreakdown
{
    public string PhaseName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MinDays { get; set; }
    public int MaxDays { get; set; }
    public List<string> Tasks { get; set; } = new();
    public List<string> Deliverables { get; set; } = new();
}
