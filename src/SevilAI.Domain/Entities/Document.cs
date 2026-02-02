namespace SevilAI.Domain.Entities;

public class Document : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<Chunk> Chunks { get; set; } = new List<Chunk>();
}
