using System.Net.Http.Json;
using NotebookLMClone.Models;

namespace NotebookLMClone.Services.VectorStore;

public sealed class ChromaVectorStore : IVectorStore
{
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;

    public ChromaVectorStore(HttpClient httpClient, string endpoint)
    {
        _httpClient = httpClient;
        _endpoint = endpoint.TrimEnd('/');
    }

    public async Task UpsertAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default)
    {
        var grouped = chunks.GroupBy(chunk => chunk.ProjectId);
        foreach (var group in grouped)
        {
            var collection = await EnsureCollectionAsync(group.Key, cancellationToken);
            var ids = new List<string>();
            var embeddings = new List<float[]>();
            var documents = new List<string>();
            var metadatas = new List<Dictionary<string, object?>>();

            foreach (var chunk in group)
            {
                ids.Add($"{chunk.DocumentId}-{chunk.PageNumber}-{chunk.ParagraphIndex}-{Guid.NewGuid():N}");
                embeddings.Add(chunk.Embedding);
                documents.Add(chunk.Text);
                metadatas.Add(new Dictionary<string, object?>
                {
                    ["documentId"] = chunk.DocumentId,
                    ["fileName"] = chunk.FileName,
                    ["pageNumber"] = chunk.PageNumber,
                    ["paragraphIndex"] = chunk.ParagraphIndex
                });
            }

            var payload = new
            {
                ids,
                embeddings,
                documents,
                metadatas
            };

            var response = await _httpClient.PostAsJsonAsync($"{_endpoint}/api/v1/collections/{collection}/add", payload, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
    }

    public async Task<IReadOnlyList<VectorSearchResult>> QueryAsync(string projectId, float[] embedding, int topK = 5, CancellationToken cancellationToken = default)
    {
        var collection = await EnsureCollectionAsync(projectId, cancellationToken);
        var payload = new
        {
            query_embeddings = new[] { embedding },
            n_results = topK,
            include = new[] { "documents", "metadatas", "distances" }
        };

        var response = await _httpClient.PostAsJsonAsync($"{_endpoint}/api/v1/collections/{collection}/query", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ChromaQueryResult>(cancellationToken: cancellationToken);
        if (result?.Documents is null || result.Metadatas is null || result.Distances is null)
        {
            return Array.Empty<VectorSearchResult>();
        }

        var results = new List<VectorSearchResult>();
        for (var i = 0; i < result.Documents[0].Count; i++)
        {
            var metadata = result.Metadatas[0][i];
            var documentChunk = new DocumentChunk(
                projectId,
                metadata["documentId"].ToString() ?? string.Empty,
                metadata["fileName"].ToString() ?? string.Empty,
                Convert.ToInt32(metadata["pageNumber"]),
                Convert.ToInt32(metadata["paragraphIndex"]),
                result.Documents[0][i],
                Array.Empty<float>());

            var score = 1f - (float)result.Distances[0][i];
            results.Add(new VectorSearchResult(documentChunk, score));
        }

        return results;
    }

    private async Task<string> EnsureCollectionAsync(string projectId, CancellationToken cancellationToken)
    {
        var collectionName = $"project_{projectId}";
        var response = await _httpClient.PostAsJsonAsync($"{_endpoint}/api/v1/collections", new { name = collectionName }, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var fallback = await _httpClient.GetAsync($"{_endpoint}/api/v1/collections/{collectionName}", cancellationToken);
            fallback.EnsureSuccessStatusCode();
        }

        return collectionName;
    }

    private sealed class ChromaQueryResult
    {
        public List<List<string>>? Documents { get; set; }
        public List<List<Dictionary<string, object>>>? Metadatas { get; set; }
        public List<List<double>>? Distances { get; set; }
    }
}
