using Microsoft.EntityFrameworkCore;
using SevilAI.Domain.Entities;
using SevilAI.Domain.Interfaces;

namespace SevilAI.Infrastructure.Persistence.Repositories;

public class ChunkRepository : IChunkRepository
{
    private readonly SevilAIDbContext _context;

    public ChunkRepository(SevilAIDbContext context)
    {
        _context = context;
    }

    public async Task<Chunk?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Chunks
            .Include(c => c.Document)
            .Include(c => c.Embedding)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Chunk>> GetByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        return await _context.Chunks
            .Where(c => c.DocumentId == documentId)
            .OrderBy(c => c.ChunkIndex)
            .ToListAsync(cancellationToken);
    }

    public async Task<Chunk> AddAsync(Chunk chunk, CancellationToken cancellationToken = default)
    {
        await _context.Chunks.AddAsync(chunk, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return chunk;
    }

    public async Task AddRangeAsync(IEnumerable<Chunk> chunks, CancellationToken cancellationToken = default)
    {
        await _context.Chunks.AddRangeAsync(chunks, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var chunks = await _context.Chunks
            .Where(c => c.DocumentId == documentId)
            .ToListAsync(cancellationToken);

        _context.Chunks.RemoveRange(chunks);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
