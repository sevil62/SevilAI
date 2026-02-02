namespace SevilAI.Domain.ValueObjects;

public record RetrievedChunk(
    Guid ChunkId,
    Guid DocumentId,
    string Content,
    string DocumentTitle,
    string SourceType,
    double SimilarityScore
);
