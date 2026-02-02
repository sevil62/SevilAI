using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SevilAI.Infrastructure.Embeddings;
using Xunit;

namespace SevilAI.Tests;

public class LocalEmbeddingServiceTests
{
    private readonly LocalEmbeddingService _sut;

    public LocalEmbeddingServiceTests()
    {
        var logger = new Mock<ILogger<LocalEmbeddingService>>();
        _sut = new LocalEmbeddingService(logger.Object, 384);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_ReturnsCorrectDimensions()
    {
        // Arrange
        var text = "This is a test text for embedding generation";

        // Act
        var result = await _sut.GenerateEmbeddingAsync(text);

        // Assert
        result.Should().HaveCount(384);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WithEmptyString_ReturnsZeroVector()
    {
        // Arrange
        var text = string.Empty;

        // Act
        var result = await _sut.GenerateEmbeddingAsync(text);

        // Assert
        result.Should().HaveCount(384);
        result.Should().OnlyContain(x => x == 0);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_SameTextProducesSameEmbedding()
    {
        // Arrange
        var text = "Test text for deterministic embedding";

        // Act
        var result1 = await _sut.GenerateEmbeddingAsync(text);
        var result2 = await _sut.GenerateEmbeddingAsync(text);

        // Assert
        result1.Should().BeEquivalentTo(result2);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_DifferentTextProducesDifferentEmbedding()
    {
        // Arrange
        var text1 = "First test text about software engineering";
        var text2 = "Completely different topic about cooking recipes";

        // Act
        var result1 = await _sut.GenerateEmbeddingAsync(text1);
        var result2 = await _sut.GenerateEmbeddingAsync(text2);

        // Assert
        result1.Should().NotBeEquivalentTo(result2);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_ReturnsNormalizedVector()
    {
        // Arrange
        var text = "Test text for normalization check";

        // Act
        var result = await _sut.GenerateEmbeddingAsync(text);

        // Assert
        var magnitude = Math.Sqrt(result.Sum(x => x * x));
        magnitude.Should().BeApproximately(1.0, 0.01);
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_ReturnsCorrectCount()
    {
        // Arrange
        var texts = new[] { "Text one", "Text two", "Text three" };

        // Act
        var result = await _sut.GenerateEmbeddingsAsync(texts);
        var resultList = result.ToList();

        // Assert
        resultList.Should().HaveCount(3);
        resultList.Should().OnlyContain(e => e.Length == 384);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_SimilarTextsHaveSimilarEmbeddings()
    {
        // Arrange
        var text1 = ".NET C# software engineering backend development";
        var text2 = "C# .NET backend software developer programming";
        var text3 = "Cooking recipes for Italian pasta dishes";

        // Act
        var emb1 = await _sut.GenerateEmbeddingAsync(text1);
        var emb2 = await _sut.GenerateEmbeddingAsync(text2);
        var emb3 = await _sut.GenerateEmbeddingAsync(text3);

        var similarity12 = CosineSimilarity(emb1, emb2);
        var similarity13 = CosineSimilarity(emb1, emb3);

        // Assert
        similarity12.Should().BeGreaterThan(similarity13);
    }

    [Fact]
    public void ModelName_ReturnsExpectedValue()
    {
        // Assert
        _sut.ModelName.Should().Be("local-hash-embedding");
    }

    [Fact]
    public void Dimensions_ReturnsConfiguredValue()
    {
        // Assert
        _sut.Dimensions.Should().Be(384);
    }

    private static double CosineSimilarity(float[] a, float[] b)
    {
        var dotProduct = a.Zip(b, (x, y) => x * y).Sum();
        var magnitudeA = Math.Sqrt(a.Sum(x => x * x));
        var magnitudeB = Math.Sqrt(b.Sum(x => x * x));
        return dotProduct / (magnitudeA * magnitudeB);
    }
}
