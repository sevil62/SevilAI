using SevilAI.Domain.Entities;

namespace SevilAI.Domain.Interfaces;

public interface IChunkRepository
{
    Task<Chunk?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Chunk>> GetByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<Chunk> AddAsync(Chunk chunk, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<Chunk> chunks, CancellationToken cancellationToken = default);
    Task DeleteByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default);
}
