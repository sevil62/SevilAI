using Microsoft.EntityFrameworkCore;
using SevilAI.Domain.Entities;
using SevilAI.Domain.Interfaces;

namespace SevilAI.Infrastructure.Persistence.Repositories;

public class QueryLogRepository : IQueryLogRepository
{
    private readonly SevilAIDbContext _context;

    public QueryLogRepository(SevilAIDbContext context)
    {
        _context = context;
    }

    public async Task<QueryLog> AddAsync(QueryLog log, CancellationToken cancellationToken = default)
    {
        await _context.QueryLogs.AddAsync(log, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return log;
    }

    public async Task<IEnumerable<QueryLog>> GetRecentAsync(int count = 100, CancellationToken cancellationToken = default)
    {
        return await _context.QueryLogs
            .OrderByDescending(l => l.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
}
