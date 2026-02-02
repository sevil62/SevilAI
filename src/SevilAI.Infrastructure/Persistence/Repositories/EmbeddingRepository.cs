using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using SevilAI.Domain.Entities;
using SevilAI.Domain.Interfaces;
using SevilAI.Domain.ValueObjects;

namespace SevilAI.Infrastructure.Persistence.Repositories;

public class EmbeddingRepository : IEmbeddingRepository
{
    private readonly SevilAIDbContext _context;

    public EmbeddingRepository(SevilAIDbContext context)
    {
        _context = context;
    }

    public async Task<Embedding?> GetByChunkIdAsync(Guid chunkId, CancellationToken cancellationToken = default)
    {
        return await _context.Embeddings
            .Include(e => e.Chunk)
            .FirstOrDefaultAsync(e => e.ChunkId == chunkId, cancellationToken);
    }

    public async Task<Embedding> AddAsync(Embedding embedding, CancellationToken cancellationToken = default)
    {
        await _context.Embeddings.AddAsync(embedding, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return embedding;
    }

    public async Task AddRangeAsync(IEnumerable<Embedding> embeddings, CancellationToken cancellationToken = default)
    {
        await _context.Embeddings.AddRangeAsync(embeddings, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<RetrievedChunk>> SearchSimilarAsync(
        float[] queryEmbedding,
        int topK = 5,
        double minSimilarity = 0.0,
        CancellationToken cancellationToken = default)
    {
        var queryVector = new Vector(queryEmbedding);

        // Load all embeddings with their chunks and documents
        // For production with large datasets, use raw SQL with pgvector operators
        var allEmbeddings = await _context.Embeddings
            .Include(e => e.Chunk)
                .ThenInclude(c => c.Document)
            .ToListAsync(cancellationToken);

        // Calculate cosine similarity in memory
        var results = allEmbeddings
            .Select(e => new
            {
                Embedding = e,
                Similarity = CosineSimilarity(queryEmbedding, e.Vector)
            })
            .OrderByDescending(x => x.Similarity)
            .Where(x => x.Similarity >= minSimilarity)
            .Take(topK)
            .Select(r => new RetrievedChunk(
                r.Embedding.ChunkId,
                r.Embedding.Chunk.DocumentId,
                r.Embedding.Chunk.Content,
                r.Embedding.Chunk.Document.Title,
                r.Embedding.Chunk.Document.SourceType,
                r.Similarity
            ))
            .ToList();

        return results;
    }

    private static double CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length || a.Length == 0)
            return 0;

        double dotProduct = 0;
        double magnitudeA = 0;
        double magnitudeB = 0;

        for (int i = 0; i < a.Length; i++)
        {
            dotProduct += a[i] * b[i];
            magnitudeA += a[i] * a[i];
            magnitudeB += b[i] * b[i];
        }

        magnitudeA = Math.Sqrt(magnitudeA);
        magnitudeB = Math.Sqrt(magnitudeB);

        if (magnitudeA == 0 || magnitudeB == 0)
            return 0;

        return dotProduct / (magnitudeA * magnitudeB);
    }

    public async Task DeleteByChunkIdAsync(Guid chunkId, CancellationToken cancellationToken = default)
    {
        var embedding = await _context.Embeddings
            .FirstOrDefaultAsync(e => e.ChunkId == chunkId, cancellationToken);

        if (embedding != null)
        {
            _context.Embeddings.Remove(embedding);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
