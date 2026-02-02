using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SevilAI.Domain.Interfaces;

namespace SevilAI.Infrastructure.LLMProviders;

public class OpenRouterSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "meta-llama/llama-3.3-70b-instruct:free";
    public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1";
    public int MaxTokens { get; set; } = 2048;
    public double Temperature { get; set; } = 0.3;
    public string SiteUrl { get; set; } = "https://sevilai.dev";
    public string SiteName { get; set; } = "SevilAI";
}

public class OpenRouterGenerator : ITextGenerator
{
    private readonly HttpClient _httpClient;
    private readonly OpenRouterSettings _settings;
    private readonly ILogger<OpenRouterGenerator> _logger;

    public OpenRouterGenerator(
        HttpClient httpClient,
        IOptions<OpenRouterSettings> settings,
        ILogger<OpenRouterGenerator> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.ApiKey}");
        _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", _settings.SiteUrl);
        _httpClient.DefaultRequestHeaders.Add("X-Title", _settings.SiteName);
    }

    public string ProviderName => "OpenRouter";
    public string ModelName => _settings.Model;

    public async Task<GenerationResult> GenerateAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new OpenRouterRequest
            {
                Model = _settings.Model,
                Messages = new[]
                {
                    new OpenRouterMessage { Role = "system", Content = systemPrompt },
                    new OpenRouterMessage { Role = "user", Content = userPrompt }
                },
                MaxTokens = _settings.MaxTokens,
                Temperature = _settings.Temperature
            };

            var response = await _httpClient.PostAsJsonAsync(
                "/chat/completions",
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("OpenRouter API error: {StatusCode} - {Error}", response.StatusCode, errorContent);

                return new GenerationResult(
                    Text: string.Empty,
                    InputTokens: 0,
                    OutputTokens: 0,
                    Success: false,
                    ErrorMessage: $"OpenRouter API error: {response.StatusCode}"
                );
            }

            var openRouterResponse = await response.Content.ReadFromJsonAsync<OpenRouterResponse>(cancellationToken: cancellationToken);

            if (openRouterResponse?.Choices == null || openRouterResponse.Choices.Length == 0)
            {
                return new GenerationResult(
                    Text: string.Empty,
                    InputTokens: 0,
                    OutputTokens: 0,
                    Success: false,
                    ErrorMessage: "Empty response from OpenRouter"
                );
            }

            var text = openRouterResponse.Choices[0].Message?.Content ?? string.Empty;
            var usage = openRouterResponse.Usage;

            return new GenerationResult(
                Text: text,
                InputTokens: usage?.PromptTokens ?? 0,
                OutputTokens: usage?.CompletionTokens ?? 0,
                Success: true
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling OpenRouter API");

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

public class OpenRouterRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public OpenRouterMessage[] Messages { get; set; } = Array.Empty<OpenRouterMessage>();

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; }

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; }
}

public class OpenRouterMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

public class OpenRouterResponse
{
    [JsonPropertyName("choices")]
    public OpenRouterChoice[]? Choices { get; set; }

    [JsonPropertyName("usage")]
    public OpenRouterUsage? Usage { get; set; }
}

public class OpenRouterChoice
{
    [JsonPropertyName("message")]
    public OpenRouterMessage? Message { get; set; }
}

public class OpenRouterUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }
}
