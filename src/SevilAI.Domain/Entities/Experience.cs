namespace SevilAI.Domain.Entities;

public class Experience : BaseEntity
{
    public string Company { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateOnly? PeriodStart { get; set; }
    public DateOnly? PeriodEnd { get; set; }
    public bool IsCurrent { get; set; }
    public string? Description { get; set; }
    public List<string> Achievements { get; set; } = new();
    public List<string> Technologies { get; set; } = new();
    public bool IsConfidential { get; set; }
    public string? NdaNote { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}
