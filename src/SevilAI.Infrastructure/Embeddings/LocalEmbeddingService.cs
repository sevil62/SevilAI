using Microsoft.Extensions.Logging;
using SevilAI.Domain.Interfaces;

namespace SevilAI.Infrastructure.Embeddings;

/// <summary>
/// A simple local embedding service that generates deterministic embeddings
/// based on text hashing. For production, replace with a proper embedding model
/// like sentence-transformers via API or local model.
/// </summary>
public class LocalEmbeddingService : IEmbeddingService
{
    private readonly ILogger<LocalEmbeddingService> _logger;
    private readonly int _dimensions;

    public LocalEmbeddingService(ILogger<LocalEmbeddingService> logger, int dimensions = 384)
    {
        _logger = logger;
        _dimensions = dimensions;
    }

    public string ModelName => "local-hash-embedding";
    public int Dimensions => _dimensions;

    public Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Task.FromResult(new float[_dimensions]);
        }

        var embedding = GenerateHashBasedEmbedding(text);
        return Task.FromResult(embedding);
    }

    public Task<IEnumerable<float[]>> GenerateEmbeddingsAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default)
    {
        var embeddings = texts.Select(t => GenerateHashBasedEmbedding(t ?? string.Empty));
        return Task.FromResult(embeddings);
    }

    /// <summary>
    /// Generates a deterministic embedding based on text characteristics.
    /// This is a simplified approach that captures:
    /// - Word frequency patterns
    /// - Character n-gram patterns
    /// - Semantic keywords
    ///
    /// For production, use a proper embedding model.
    /// </summary>
    private float[] GenerateHashBasedEmbedding(string text)
    {
        var embedding = new float[_dimensions];
        var normalizedText = text.ToLowerInvariant();

        // Layer 1: Character-level features (first 100 dimensions)
        var charFeatures = GetCharacterFeatures(normalizedText);
        for (int i = 0; i < Math.Min(100, charFeatures.Length); i++)
        {
            embedding[i] = charFeatures[i];
        }

        // Layer 2: Word-level features (next 100 dimensions)
        var words = normalizedText.Split(new[] { ' ', '\n', '\t', '.', ',', '!', '?' },
            StringSplitOptions.RemoveEmptyEntries);
        var wordFeatures = GetWordFeatures(words);
        for (int i = 0; i < Math.Min(100, wordFeatures.Length); i++)
        {
            embedding[100 + i] = wordFeatures[i];
        }

        // Layer 3: Semantic keyword features (next 100 dimensions)
        var keywordFeatures = GetKeywordFeatures(normalizedText, words);
        for (int i = 0; i < Math.Min(100, keywordFeatures.Length); i++)
        {
            embedding[200 + i] = keywordFeatures[i];
        }

        // Layer 4: N-gram features (remaining dimensions)
        var ngramFeatures = GetNgramFeatures(normalizedText);
        for (int i = 0; i < Math.Min(84, ngramFeatures.Length); i++)
        {
            embedding[300 + i] = ngramFeatures[i];
        }

        // Normalize the embedding vector
        NormalizeVector(embedding);

        return embedding;
    }

    private float[] GetCharacterFeatures(string text)
    {
        var features = new float[100];

        if (string.IsNullOrEmpty(text)) return features;

        // Character frequency distribution
        for (int i = 0; i < text.Length && i < 100; i++)
        {
            var charCode = (int)text[i];
            var index = charCode % 100;
            features[index] += 0.1f;
        }

        // Letter frequency
        foreach (var c in text.Where(char.IsLetter))
        {
            var index = (c - 'a') % 26;
            if (index >= 0 && index < 26)
            {
                features[index] += 0.05f;
            }
        }

        return features;
    }

    private float[] GetWordFeatures(string[] words)
    {
        var features = new float[100];

        if (words.Length == 0) return features;

        // Word length distribution
        foreach (var word in words)
        {
            var lengthBucket = Math.Min(word.Length / 2, 9);
            features[lengthBucket] += 0.1f;
        }

        // Word count feature
        features[10] = Math.Min(words.Length / 100f, 1f);

        // Average word length
        features[11] = (float)(words.Average(w => w.Length) / 20.0);

        // Unique word ratio
        features[12] = (float)words.Distinct().Count() / words.Length;

        // Hash-based word features
        foreach (var word in words.Distinct())
        {
            var hash = word.GetHashCode();
            var index = Math.Abs(hash) % 87 + 13;
            features[index] += 0.1f / words.Length;
        }

        return features;
    }

    private float[] GetKeywordFeatures(string text, string[] words)
    {
        var features = new float[100];

        // Domain-specific keyword detection
        var keywordGroups = new Dictionary<int, string[]>
        {
            [0] = new[] { ".net", "c#", "csharp", "asp.net", "dotnet" },
            [5] = new[] { "software", "engineer", "developer", "programmer" },
            [10] = new[] { "backend", "api", "service", "server" },
            [15] = new[] { "database", "sql", "postgresql", "data" },
            [20] = new[] { "architecture", "design", "pattern", "system" },
            [25] = new[] { "test", "testing", "unit", "integration" },
            [30] = new[] { "docker", "container", "kubernetes", "deploy" },
            [35] = new[] { "experience", "work", "job", "career", "company" },
            [40] = new[] { "skill", "technology", "framework", "tool" },
            [45] = new[] { "project", "application", "solution", "product" },
            [50] = new[] { "team", "lead", "manage", "collaborate" },
            [55] = new[] { "microservice", "distributed", "event", "saga" },
            [60] = new[] { "security", "authentication", "authorization" },
            [65] = new[] { "performance", "optimization", "scalable" },
            [70] = new[] { "frontend", "ui", "ux", "interface" },
            [75] = new[] { "devexpress", "winforms", "wpf", "desktop" },
            [80] = new[] { "json", "xml", "protocol", "format" },
            [85] = new[] { "configuration", "validation", "parsing" },
            [90] = new[] { "arinc", "aviation", "defense", "mission" },
            [95] = new[] { "estimate", "effort", "days", "planning" }
        };

        foreach (var (startIndex, keywords) in keywordGroups)
        {
            foreach (var keyword in keywords)
            {
                if (text.Contains(keyword))
                {
                    var keywordIndex = startIndex + Array.IndexOf(keywords, keyword);
                    if (keywordIndex < 100)
                    {
                        features[keywordIndex] = 1f;
                    }
                }
            }
        }

        return features;
    }

    private float[] GetNgramFeatures(string text)
    {
        var features = new float[84];

        if (text.Length < 3) return features;

        // Generate 3-character n-grams
        for (int i = 0; i < text.Length - 2; i++)
        {
            var ngram = text.Substring(i, 3);
            var hash = ngram.GetHashCode();
            var index = Math.Abs(hash) % 84;
            features[index] += 0.05f;
        }

        return features;
    }

    private void NormalizeVector(float[] vector)
    {
        var magnitude = (float)Math.Sqrt(vector.Sum(x => x * x));

        if (magnitude > 0)
        {
            for (int i = 0; i < vector.Length; i++)
            {
                vector[i] /= magnitude;
            }
        }
    }
}
