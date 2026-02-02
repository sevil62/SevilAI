namespace SevilAI.Domain.Entities;

public class Embedding : BaseEntity
{
    public Guid ChunkId { get; set; }
    public float[] Vector { get; set; } = Array.Empty<float>();
    public string ModelName { get; set; } = string.Empty;

    public virtual Chunk Chunk { get; set; } = null!;
}
