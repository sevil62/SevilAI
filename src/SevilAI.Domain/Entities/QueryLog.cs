using SevilAI.Domain.Enums;

namespace SevilAI.Domain.Entities;

public class QueryLog : BaseEntity
{
    public string QueryText { get; set; } = string.Empty;
    public string? ResponseText { get; set; }
    public List<Guid> ChunksUsed { get; set; } = new();
    public decimal? ConfidenceScore { get; set; }
    public GenerationMode GenerationMode { get; set; }
    public int? LatencyMs { get; set; }
}
