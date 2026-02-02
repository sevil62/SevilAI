using SevilAI.Domain.Enums;

namespace SevilAI.Domain.Entities;

public class Skill : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public SkillCategory Category { get; set; }
    public ProficiencyLevel? ProficiencyLevel { get; set; }
    public decimal? YearsExperience { get; set; }
    public string? Description { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}
