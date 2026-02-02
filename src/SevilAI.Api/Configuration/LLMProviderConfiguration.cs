using SevilAI.Domain.Interfaces;
using SevilAI.Infrastructure.LLMProviders;

namespace SevilAI.Api.Configuration;

public static class LLMProviderConfiguration
{
    public static IServiceCollection ConfigureLLMProvider(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var llmSettings = configuration.GetSection("LLMSettings");
        var provider = llmSettings.GetValue<string>("Provider")?.ToLowerInvariant() ?? "none";

        switch (provider)
        {
            case "groq":
                ConfigureGroq(services, configuration);
                break;

            case "openrouter":
                ConfigureOpenRouter(services, configuration);
                break;

            case "gemini":
                ConfigureGemini(services, configuration);
                break;

            default:
                services.AddScoped<ITextGenerator, NoLLMGenerator>();
                break;
        }

        return services;
    }

    private static void ConfigureGroq(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<GroqSettings>(configuration.GetSection("LLMSettings:Groq"));

        services.AddHttpClient<ITextGenerator, GroqGenerator>()
            .ConfigureHttpClient(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(60);
            });
    }

    private static void ConfigureOpenRouter(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OpenRouterSettings>(configuration.GetSection("LLMSettings:OpenRouter"));

        services.AddHttpClient<ITextGenerator, OpenRouterGenerator>()
            .ConfigureHttpClient(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(60);
            });
    }

    private static void ConfigureGemini(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<GeminiSettings>(configuration.GetSection("LLMSettings:Gemini"));

        services.AddHttpClient<ITextGenerator, GeminiGenerator>()
            .ConfigureHttpClient(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(60);
            });
    }
}
