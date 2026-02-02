using Microsoft.EntityFrameworkCore;
using SevilAI.Domain.Entities;
using SevilAI.Domain.Interfaces;

namespace SevilAI.Infrastructure.Persistence.Repositories;

public class ExperienceRepository : IExperienceRepository
{
    private readonly SevilAIDbContext _context;

    public ExperienceRepository(SevilAIDbContext context)
    {
        _context = context;
    }

    public async Task<Experience?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Experiences.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<IEnumerable<Experience>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Experiences
            .OrderByDescending(e => e.PeriodStart)
            .ToListAsync(cancellationToken);
    }

    public async Task<Experience?> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Experiences
            .FirstOrDefaultAsync(e => e.IsCurrent, cancellationToken);
    }

    public async Task<Experience> AddAsync(Experience experience, CancellationToken cancellationToken = default)
    {
        await _context.Experiences.AddAsync(experience, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return experience;
    }

    public async Task AddRangeAsync(IEnumerable<Experience> experiences, CancellationToken cancellationToken = default)
    {
        await _context.Experiences.AddRangeAsync(experiences, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Experience experience, CancellationToken cancellationToken = default)
    {
        _context.Experiences.Update(experience);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var experience = await _context.Experiences.FindAsync(new object[] { id }, cancellationToken);
        if (experience != null)
        {
            _context.Experiences.Remove(experience);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
