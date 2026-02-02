namespace SevilAI.Application.Interfaces;

public interface IChunkingService
{
    IEnumerable<TextChunk> ChunkText(string text, int maxTokens = 500, int overlap = 50);
    int CountTokens(string text);
}

public record TextChunk(
    string Content,
    int Index,
    int TokenCount,
    int StartPosition,
    int EndPosition
);
