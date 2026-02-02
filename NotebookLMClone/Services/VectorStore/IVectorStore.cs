using NotebookLMClone.Models;

namespace NotebookLMClone.Services.VectorStore;

public interface IVectorStore
{
    Task UpsertAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VectorSearchResult>> QueryAsync(string projectId, float[] embedding, int topK = 5, CancellationToken cancellationToken = default);
}

public sealed record VectorSearchResult(DocumentChunk Chunk, float Score);
