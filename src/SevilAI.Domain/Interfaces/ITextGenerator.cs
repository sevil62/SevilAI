namespace SevilAI.Domain.Interfaces;

public interface ITextGenerator
{
    Task<GenerationResult> GenerateAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default);

    string ProviderName { get; }
    string ModelName { get; }
}

public record GenerationResult(
    string Text,
    int InputTokens,
    int OutputTokens,
    bool Success,
    string? ErrorMessage = null
);
