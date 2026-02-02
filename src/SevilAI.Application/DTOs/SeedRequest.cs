namespace SevilAI.Application.DTOs;

public class SeedRequest
{
    public string? JsonData { get; set; }
    public bool ClearExisting { get; set; } = false;
}

public class SeedResponse
{
    public bool Success { get; set; }
    public int DocumentsCreated { get; set; }
    public int ChunksCreated { get; set; }
    public int EmbeddingsCreated { get; set; }
    public int SkillsCreated { get; set; }
    public int ExperiencesCreated { get; set; }
    public int ProjectsCreated { get; set; }
    public List<string> Errors { get; set; } = new();
    public int DurationMs { get; set; }
}
