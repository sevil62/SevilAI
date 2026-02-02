using System.Text;
using System.Text.RegularExpressions;
using SevilAI.Application.Interfaces;

namespace SevilAI.Application.Services;

public partial class ChunkingService : IChunkingService
{
    // Approximate tokens per character for English text
    private const double TokensPerChar = 0.25;

    public IEnumerable<TextChunk> ChunkText(string text, int maxTokens = 500, int overlap = 50)
    {
        if (string.IsNullOrWhiteSpace(text))
            yield break;

        // Clean and normalize text
        var cleanedText = CleanText(text);

        // Split into sentences first for better semantic coherence
        var sentences = SplitIntoSentences(cleanedText);

        var currentChunk = new StringBuilder();
        var chunkIndex = 0;
        var startPosition = 0;
        var currentTokens = 0;
        var overlapBuffer = new Queue<string>();

        foreach (var sentence in sentences)
        {
            var sentenceTokens = CountTokens(sentence);

            // If single sentence exceeds max tokens, split by words
            if (sentenceTokens > maxTokens)
            {
                // Yield current chunk if not empty
                if (currentChunk.Length > 0)
                {
                    var content = currentChunk.ToString().Trim();
                    yield return new TextChunk(
                        content,
                        chunkIndex++,
                        CountTokens(content),
                        startPosition,
                        startPosition + content.Length
                    );
                    startPosition += content.Length;
                    currentChunk.Clear();
                    currentTokens = 0;
                }

                // Split long sentence into word-based chunks
                var longChunks = SplitLongSentence(sentence, maxTokens, overlap, chunkIndex, startPosition);
                foreach (var chunk in longChunks)
                {
                    yield return chunk;
                    chunkIndex = chunk.Index + 1;
                    startPosition = chunk.EndPosition;
                }
                continue;
            }

            // Check if adding this sentence would exceed limit
            if (currentTokens + sentenceTokens > maxTokens && currentChunk.Length > 0)
            {
                var content = currentChunk.ToString().Trim();
                yield return new TextChunk(
                    content,
                    chunkIndex++,
                    CountTokens(content),
                    startPosition,
                    startPosition + content.Length
                );

                // Handle overlap - keep last few sentences
                var overlapText = new StringBuilder();
                while (overlapBuffer.Count > 0 && CountTokens(overlapText.ToString()) < overlap)
                {
                    overlapText.Insert(0, overlapBuffer.Dequeue() + " ");
                }

                startPosition += content.Length - overlapText.Length;
                currentChunk.Clear();
                currentChunk.Append(overlapText);
                currentTokens = CountTokens(overlapText.ToString());
                overlapBuffer.Clear();
            }

            currentChunk.Append(sentence);
            currentChunk.Append(' ');
            currentTokens += sentenceTokens;

            // Maintain overlap buffer
            overlapBuffer.Enqueue(sentence);
            if (overlapBuffer.Count > 3) overlapBuffer.Dequeue();
        }

        // Yield remaining content
        if (currentChunk.Length > 0)
        {
            var content = currentChunk.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(content))
            {
                yield return new TextChunk(
                    content,
                    chunkIndex,
                    CountTokens(content),
                    startPosition,
                    startPosition + content.Length
                );
            }
        }
    }

    public int CountTokens(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        // Simple approximation: split by whitespace and punctuation
        // For production, consider using a proper tokenizer like tiktoken
        var words = WordSplitRegex().Split(text)
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .ToList();

        // Approximate: 1 word ≈ 1.3 tokens on average
        return (int)Math.Ceiling(words.Count * 1.3);
    }

    private static string CleanText(string text)
    {
        // Remove excessive whitespace
        text = WhitespaceRegex().Replace(text, " ");

        // Normalize line endings
        text = text.Replace("\r\n", "\n").Replace("\r", "\n");

        // Remove multiple newlines
        text = MultipleNewlineRegex().Replace(text, "\n\n");

        return text.Trim();
    }

    private static IEnumerable<string> SplitIntoSentences(string text)
    {
        // Split on sentence-ending punctuation followed by space or newline
        var sentences = SentenceSplitRegex().Split(text)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s));

        return sentences;
    }

    private List<TextChunk> SplitLongSentence(
        string sentence,
        int maxTokens,
        int overlap,
        int startChunkIndex,
        int startPos)
    {
        var results = new List<TextChunk>();
        var words = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var currentChunk = new StringBuilder();
        var currentTokens = 0;
        var chunkIndex = startChunkIndex;
        var position = startPos;

        for (int i = 0; i < words.Length; i++)
        {
            var wordTokens = CountTokens(words[i]);

            if (currentTokens + wordTokens > maxTokens && currentChunk.Length > 0)
            {
                var content = currentChunk.ToString().Trim();
                results.Add(new TextChunk(
                    content,
                    chunkIndex++,
                    CountTokens(content),
                    position,
                    position + content.Length
                ));

                position += content.Length;
                currentChunk.Clear();
                currentTokens = 0;

                // Add overlap from previous words
                var overlapStart = Math.Max(0, i - 3);
                for (int j = overlapStart; j < i; j++)
                {
                    currentChunk.Append(words[j]);
                    currentChunk.Append(' ');
                    currentTokens += CountTokens(words[j]);
                }
            }

            currentChunk.Append(words[i]);
            currentChunk.Append(' ');
            currentTokens += wordTokens;
        }

        if (currentChunk.Length > 0)
        {
            var content = currentChunk.ToString().Trim();
            results.Add(new TextChunk(
                content,
                chunkIndex,
                CountTokens(content),
                position,
                position + content.Length
            ));
        }

        return results;
    }

    [GeneratedRegex(@"[\s]+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex MultipleNewlineRegex();

    [GeneratedRegex(@"(?<=[.!?])\s+")]
    private static partial Regex SentenceSplitRegex();

    [GeneratedRegex(@"[\s\p{P}]+")]
    private static partial Regex WordSplitRegex();
}
