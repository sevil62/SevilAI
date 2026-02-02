using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SevilAI.Domain.Interfaces;

namespace SevilAI.Infrastructure.LLMProviders;

public class GroqSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "llama-3.3-70b-versatile";
    public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1";
    public int MaxTokens { get; set; } = 2048;
    public double Temperature { get; set; } = 0.3;
}

public class GroqGenerator : ITextGenerator
{
    private readonly HttpClient _httpClient;
    private readonly GroqSettings _settings;
    private readonly ILogger<GroqGenerator> _logger;

    public GroqGenerator(
        HttpClient httpClient,
        IOptions<GroqSettings> settings,
        ILogger<GroqGenerator> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        var baseUrl = _settings.BaseUrl.TrimEnd('/') + "/";
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.ApiKey}");
    }

    public string ProviderName => "Groq";
    public string ModelName => _settings.Model;

    public async Task<GenerationResult> GenerateAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new GroqRequest
            {
                Model = _settings.Model,
                Messages = new[]
                {
                    new GroqMessage { Role = "system", Content = systemPrompt },
                    new GroqMessage { Role = "user", Content = userPrompt }
                },
                MaxTokens = _settings.MaxTokens,
                Temperature = _settings.Temperature
            };

            var response = await _httpClient.PostAsJsonAsync(
                "chat/completions",
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Groq API error: {StatusCode} - {Error}", response.StatusCode, errorContent);

                return new GenerationResult(
                    Text: string.Empty,
                    InputTokens: 0,
                    OutputTokens: 0,
                    Success: false,
                    ErrorMessage: $"Groq API error: {response.StatusCode}"
                );
            }

            var groqResponse = await response.Content.ReadFromJsonAsync<GroqResponse>(cancellationToken: cancellationToken);

            if (groqResponse?.Choices == null || groqResponse.Choices.Length == 0)
            {
                return new GenerationResult(
                    Text: string.Empty,
                    InputTokens: 0,
                    OutputTokens: 0,
                    Success: false,
                    ErrorMessage: "Empty response from Groq"
                );
            }

            var text = groqResponse.Choices[0].Message?.Content ?? string.Empty;
            var usage = groqResponse.Usage;

            return new GenerationResult(
                Text: text,
                InputTokens: usage?.PromptTokens ?? 0,
                OutputTokens: usage?.CompletionTokens ?? 0,
                Success: true
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Groq API");

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

public class GroqRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public GroqMessage[] Messages { get; set; } = Array.Empty<GroqMessage>();

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; }

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; }
}

public class GroqMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

public class GroqResponse
{
    [JsonPropertyName("choices")]
    public GroqChoice[]? Choices { get; set; }

    [JsonPropertyName("usage")]
    public GroqUsage? Usage { get; set; }
}

public class GroqChoice
{
    [JsonPropertyName("message")]
    public GroqMessage? Message { get; set; }
}

public class GroqUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }
}
