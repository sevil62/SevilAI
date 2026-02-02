namespace SevilAI.Domain.Entities;

public class Chunk : BaseEntity
{
    public Guid DocumentId { get; set; }
    public int ChunkIndex { get; set; }
    public string Content { get; set; } = string.Empty;
    public int TokenCount { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();

    public virtual Document Document { get; set; } = null!;
    public virtual Embedding? Embedding { get; set; }
}
