using System.ComponentModel.DataAnnotations;

namespace SevilAI.Application.DTOs;

public class EstimateRequest
{
    [Required]
    [MinLength(10)]
    [MaxLength(5000)]
    public string ProjectDescription { get; set; } = string.Empty;

    public List<string> RequiredFeatures { get; set; } = new();

    public List<string> TechStack { get; set; } = new();

    public List<string> Constraints { get; set; } = new();

    public string? TeamSize { get; set; }

    public bool DetailedBreakdown { get; set; } = true;
}
