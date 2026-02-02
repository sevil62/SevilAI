using Microsoft.EntityFrameworkCore;
using SevilAI.Domain.Entities;
using SevilAI.Domain.Enums;
using SevilAI.Domain.Interfaces;

namespace SevilAI.Infrastructure.Persistence.Repositories;

public class SkillRepository : ISkillRepository
{
    private readonly SevilAIDbContext _context;

    public SkillRepository(SevilAIDbContext context)
    {
        _context = context;
    }

    public async Task<Skill?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Skills.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<Skill?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Skills
            .FirstOrDefaultAsync(s => s.Name == name, cancellationToken);
    }

    public async Task<IEnumerable<Skill>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Skills.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Skill>> GetByCategoryAsync(SkillCategory category, CancellationToken cancellationToken = default)
    {
        return await _context.Skills
            .Where(s => s.Category == category)
            .ToListAsync(cancellationToken);
    }

    public async Task<Skill> AddAsync(Skill skill, CancellationToken cancellationToken = default)
    {
        await _context.Skills.AddAsync(skill, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return skill;
    }

    public async Task AddRangeAsync(IEnumerable<Skill> skills, CancellationToken cancellationToken = default)
    {
        await _context.Skills.AddRangeAsync(skills, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Skill skill, CancellationToken cancellationToken = default)
    {
        _context.Skills.Update(skill);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var skill = await _context.Skills.FindAsync(new object[] { id }, cancellationToken);
        if (skill != null)
        {
            _context.Skills.Remove(skill);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
