using SevilAI.Domain.Entities;

namespace SevilAI.Domain.Interfaces;

public interface IQueryLogRepository
{
    Task<QueryLog> AddAsync(QueryLog log, CancellationToken cancellationToken = default);
    Task<IEnumerable<QueryLog>> GetRecentAsync(int count = 100, CancellationToken cancellationToken = default);
}
