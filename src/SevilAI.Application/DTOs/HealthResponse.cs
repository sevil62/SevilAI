namespace SevilAI.Application.DTOs;

public class HealthResponse
{
    public string Status { get; set; } = "healthy";
    public string Version { get; set; } = "1.0.0";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public DatabaseHealth Database { get; set; } = new();
    public LLMHealth LLM { get; set; } = new();
    public EmbeddingHealth Embedding { get; set; } = new();
    public KnowledgeBaseStats KnowledgeBase { get; set; } = new();
}

public class DatabaseHealth
{
    public bool Connected { get; set; }
    public int LatencyMs { get; set; }
}

public class LLMHealth
{
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public bool Available { get; set; }
}

public class EmbeddingHealth
{
    public string Model { get; set; } = string.Empty;
    public int Dimensions { get; set; }
    public bool Available { get; set; }
}

public class KnowledgeBaseStats
{
    public int TotalDocuments { get; set; }
    public int TotalChunks { get; set; }
    public int TotalSkills { get; set; }
    public int TotalExperiences { get; set; }
    public int TotalProjects { get; set; }
}
