using NotebookLMClone.Models;

namespace NotebookLMClone.Services.VectorStore;

public sealed class InMemoryVectorStore : IVectorStore
{
    private readonly List<DocumentChunk> _chunks = new();

    public Task UpsertAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default)
    {
        _chunks.AddRange(chunks);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<VectorSearchResult>> QueryAsync(string projectId, float[] embedding, int topK = 5, CancellationToken cancellationToken = default)
    {
        var results = _chunks
            .Where(chunk => chunk.ProjectId == projectId)
            .Select(chunk => new VectorSearchResult(chunk, CosineSimilarity(chunk.Embedding, embedding)))
            .OrderByDescending(result => result.Score)
            .Take(topK)
            .ToList();

        return Task.FromResult<IReadOnlyList<VectorSearchResult>>(results);
    }

    private static float CosineSimilarity(float[] left, float[] right)
    {
        var minLength = Math.Min(left.Length, right.Length);
        var dot = 0.0;
        var leftSum = 0.0;
        var rightSum = 0.0;
        for (var i = 0; i < minLength; i++)
        {
            dot += left[i] * right[i];
            leftSum += left[i] * left[i];
            rightSum += right[i] * right[i];
        }

        return (float)(dot / (Math.Sqrt(leftSum) * Math.Sqrt(rightSum) + 1e-8));
    }
}
