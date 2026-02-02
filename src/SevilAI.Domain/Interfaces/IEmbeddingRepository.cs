using SevilAI.Domain.Entities;
using SevilAI.Domain.ValueObjects;

namespace SevilAI.Domain.Interfaces;

public interface IEmbeddingRepository
{
    Task<Embedding?> GetByChunkIdAsync(Guid chunkId, CancellationToken cancellationToken = default);
    Task<Embedding> AddAsync(Embedding embedding, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<Embedding> embeddings, CancellationToken cancellationToken = default);
    Task<IEnumerable<RetrievedChunk>> SearchSimilarAsync(
        float[] queryEmbedding,
        int topK = 5,
        double minSimilarity = 0.0,
        CancellationToken cancellationToken = default);
    Task DeleteByChunkIdAsync(Guid chunkId, CancellationToken cancellationToken = default);
}
