using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SevilAI.Domain.Interfaces;

namespace SevilAI.Infrastructure.LLMProviders;

public class GeminiSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gemini-1.5-flash";
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";
    public int MaxTokens { get; set; } = 2048;
    public double Temperature { get; set; } = 0.3;
}

public class GeminiGenerator : ITextGenerator
{
    private readonly HttpClient _httpClient;
    private readonly GeminiSettings _settings;
    private readonly ILogger<GeminiGenerator> _logger;

    public GeminiGenerator(
        HttpClient httpClient,
        IOptions<GeminiSettings> settings,
        ILogger<GeminiGenerator> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
    }

    public string ProviderName => "Gemini";
    public string ModelName => _settings.Model;

    public async Task<GenerationResult> GenerateAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new GeminiRequest
            {
                Contents = new[]
                {
                    new GeminiContent
                    {
                        Parts = new[]
                        {
                            new GeminiPart { Text = systemPrompt + "\n\n" + userPrompt }
                        }
                    }
                },
                GenerationConfig = new GeminiGenerationConfig
                {
                    MaxOutputTokens = _settings.MaxTokens,
                    Temperature = _settings.Temperature
                }
            };

            var url = $"/models/{_settings.Model}:generateContent?key={_settings.ApiKey}";

            var response = await _httpClient.PostAsJsonAsync(url, request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Gemini API error: {StatusCode} - {Error}", response.StatusCode, errorContent);

                return new GenerationResult(
                    Text: string.Empty,
                    InputTokens: 0,
                    OutputTokens: 0,
                    Success: false,
                    ErrorMessage: $"Gemini API error: {response.StatusCode}"
                );
            }

            var geminiResponse = await response.Content.ReadFromJsonAsync<GeminiResponse>(cancellationToken: cancellationToken);

            if (geminiResponse?.Candidates == null || geminiResponse.Candidates.Length == 0)
            {
                return new GenerationResult(
                    Text: string.Empty,
                    InputTokens: 0,
                    OutputTokens: 0,
                    Success: false,
                    ErrorMessage: "Empty response from Gemini"
                );
            }

            var text = geminiResponse.Candidates[0].Content?.Parts?.FirstOrDefault()?.Text ?? string.Empty;
            var usage = geminiResponse.UsageMetadata;

            return new GenerationResult(
                Text: text,
                InputTokens: usage?.PromptTokenCount ?? 0,
                OutputTokens: usage?.CandidatesTokenCount ?? 0,
                Success: true
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Gemini API");

            return new GenerationResult(
                Text: string.Empty,
                InputTokens: 0,
                OutputTokens: 0,
                Success: false,
                ErrorMessage: ex.Message
            );
        }
    }
}

public class GeminiRequest
{
    [JsonPropertyName("contents")]
    public GeminiContent[] Contents { get; set; } = Array.Empty<GeminiContent>();

    [JsonPropertyName("generationConfig")]
    public GeminiGenerationConfig? GenerationConfig { get; set; }
}

public class GeminiContent
{
    [JsonPropertyName("parts")]
    public GeminiPart[] Parts { get; set; } = Array.Empty<GeminiPart>();
}

public class GeminiPart
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

public class GeminiGenerationConfig
{
    [JsonPropertyName("maxOutputTokens")]
    public int MaxOutputTokens { get; set; }

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; }
}

public class GeminiResponse
{
    [JsonPropertyName("candidates")]
    public GeminiCandidate[]? Candidates { get; set; }

    [JsonPropertyName("usageMetadata")]
    public GeminiUsageMetadata? UsageMetadata { get; set; }
}

public class GeminiCandidate
{
    [JsonPropertyName("content")]
    public GeminiContent? Content { get; set; }
}

public class GeminiUsageMetadata
{
    [JsonPropertyName("promptTokenCount")]
    public int PromptTokenCount { get; set; }

    [JsonPropertyName("candidatesTokenCount")]
    public int CandidatesTokenCount { get; set; }
}
