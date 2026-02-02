using SevilAI.Domain.Enums;

namespace SevilAI.Domain.Entities;

public class Project : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public ProjectType ProjectType { get; set; }
    public string? Description { get; set; }
    public List<string> Technologies { get; set; } = new();
    public List<string> Features { get; set; } = new();
    public string? ArchitectureNotes { get; set; }
    public bool IsConfidential { get; set; }
    public string? NdaNote { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}
