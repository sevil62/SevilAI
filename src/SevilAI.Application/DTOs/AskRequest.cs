using System.ComponentModel.DataAnnotations;

namespace SevilAI.Application.DTOs;

public class AskRequest
{
    [Required]
    [MinLength(3)]
    [MaxLength(2000)]
    public string Question { get; set; } = string.Empty;

    public int TopK { get; set; } = 5;

    public double MinSimilarity { get; set; } = 0.3;

    public bool UseLLM { get; set; } = true;

    public bool IncludeSources { get; set; } = true;
}
