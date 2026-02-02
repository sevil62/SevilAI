namespace SevilAI.Application.DTOs;

public class AskResponse
{
    public string Answer { get; set; } = string.Empty;
    public decimal ConfidenceScore { get; set; }
    public string GenerationMode { get; set; } = string.Empty;
    public List<SourceSnippet> Sources { get; set; } = new();
    public int LatencyMs { get; set; }
    public QueryMetadata Metadata { get; set; } = new();
}

public class SourceSnippet
{
    public string DocumentTitle { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public double SimilarityScore { get; set; }
}

public class QueryMetadata
{
    public int ChunksRetrieved { get; set; }
    public int TokensUsed { get; set; }
    public string Model { get; set; } = string.Empty;
}
