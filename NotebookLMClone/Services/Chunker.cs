namespace NotebookLMClone.Services;

public sealed class Chunker
{
    private readonly int _maxChunkLength;

    public Chunker(int maxChunkLength = 1000)
    {
        _maxChunkLength = maxChunkLength;
    }

    public IReadOnlyList<ChunkInput> CreateChunks(IEnumerable<ParsedSegment> segments)
    {
        var results = new List<ChunkInput>();
        foreach (var segment in segments)
        {
            if (segment.Text.Length <= _maxChunkLength)
            {
                results.Add(new ChunkInput(segment.Text, segment.PageNumber, segment.ParagraphIndex));
                continue;
            }

            var start = 0;
            while (start < segment.Text.Length)
            {
                var length = Math.Min(_maxChunkLength, segment.Text.Length - start);
                var chunkText = segment.Text.Substring(start, length);
                results.Add(new ChunkInput(chunkText, segment.PageNumber, segment.ParagraphIndex));
                start += length;
            }
        }

        return results;
    }
}

public sealed record ChunkInput(string Text, int PageNumber, int ParagraphIndex);
