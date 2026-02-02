using FluentAssertions;
using SevilAI.Application.Services;
using Xunit;

namespace SevilAI.Tests;

public class ChunkingServiceTests
{
    private readonly ChunkingService _sut;

    public ChunkingServiceTests()
    {
        _sut = new ChunkingService();
    }

    [Fact]
    public void ChunkText_WithEmptyString_ReturnsNoChunks()
    {
        // Arrange
        var text = string.Empty;

        // Act
        var result = _sut.ChunkText(text).ToList();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ChunkText_WithShortText_ReturnsSingleChunk()
    {
        // Arrange
        var text = "This is a short text that should fit in one chunk.";

        // Act
        var result = _sut.ChunkText(text, maxTokens: 500).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].Content.Should().Be(text);
        result[0].Index.Should().Be(0);
    }

    [Fact]
    public void ChunkText_WithLongText_ReturnsMultipleChunks()
    {
        // Arrange
        var text = string.Join(" ", Enumerable.Repeat("This is a test sentence that will be repeated many times to create a long text.", 50));

        // Act
        var result = _sut.ChunkText(text, maxTokens: 100, overlap: 20).ToList();

        // Assert
        result.Should().HaveCountGreaterThan(1);
        result.Should().OnlyContain(c => c.TokenCount <= 130); // Allow some margin for overlap
        result.Select(c => c.Index).Should().BeInAscendingOrder();
    }

    [Fact]
    public void ChunkText_PreservesSentenceBoundaries()
    {
        // Arrange
        var text = "First sentence. Second sentence. Third sentence. Fourth sentence.";

        // Act
        var result = _sut.ChunkText(text, maxTokens: 500).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].Content.Should().Contain("First sentence");
        result[0].Content.Should().Contain("Fourth sentence");
    }

    [Fact]
    public void CountTokens_WithEmptyString_ReturnsZero()
    {
        // Arrange
        var text = string.Empty;

        // Act
        var result = _sut.CountTokens(text);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void CountTokens_WithText_ReturnsApproximateCount()
    {
        // Arrange
        var text = "This is a test with five words";

        // Act
        var result = _sut.CountTokens(text);

        // Assert
        result.Should().BeGreaterThan(0);
        result.Should().BeLessThan(20); // Reasonable upper bound
    }

    [Fact]
    public void ChunkText_ChunksHaveCorrectIndices()
    {
        // Arrange
        var text = string.Join(" ", Enumerable.Repeat("Word", 500));

        // Act
        var result = _sut.ChunkText(text, maxTokens: 50).ToList();

        // Assert
        for (int i = 0; i < result.Count; i++)
        {
            result[i].Index.Should().Be(i);
        }
    }
}
