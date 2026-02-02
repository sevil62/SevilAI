using SevilAI.Domain.Interfaces;

namespace SevilAI.Infrastructure.LLMProviders;

/// <summary>
/// Fallback generator that returns a template response when no LLM is configured.
/// </summary>
public class NoLLMGenerator : ITextGenerator
{
    public string ProviderName => "NoLLM";
    public string ModelName => "none";

    public Task<GenerationResult> GenerateAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        // Return a message indicating LLM is not configured
        var result = new GenerationResult(
            Text: "LLM generation is not configured. Using template-based response mode. " +
                  "To enable LLM-powered responses, configure a provider (Groq, OpenRouter, or Gemini) " +
                  "in the application settings.",
            InputTokens: 0,
            OutputTokens: 0,
            Success: false,
            ErrorMessage: "No LLM provider configured"
        );

        return Task.FromResult(result);
    }
}
