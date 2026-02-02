using SevilAI.Domain.Entities;

namespace SevilAI.Domain.Interfaces;

public interface IExperienceRepository
{
    Task<Experience?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Experience>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Experience?> GetCurrentAsync(CancellationToken cancellationToken = default);
    Task<Experience> AddAsync(Experience experience, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<Experience> experiences, CancellationToken cancellationToken = default);
    Task UpdateAsync(Experience experience, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
