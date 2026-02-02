using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace NotebookLMClone.Services;

public sealed class GeminiClient
{
    private readonly HttpClient _httpClient;
    private readonly AppSettings _settings;

    public GeminiClient(HttpClient httpClient, AppSettings settings)
    {
        _httpClient = httpClient;
        _settings = settings;
    }

    public async Task<float[]> CreateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var url = $"{_settings.GeminiBaseUrl}/{_settings.GeminiEmbeddingModel}:embedContent?key={_settings.GeminiApiKey}";
        var payload = new
        {
            content = new
            {
                parts = new[] { new { text } }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        var embedding = doc.RootElement.GetProperty("embedding").GetProperty("values");
        var result = new float[embedding.GetArrayLength()];
        var index = 0;
        foreach (var value in embedding.EnumerateArray())
        {
            result[index++] = value.GetSingle();
        }

        return result;
    }

    public async Task<string> GenerateAnswerAsync(string question, IEnumerable<string> contextChunks, CancellationToken cancellationToken = default)
    {
        var url = $"{_settings.GeminiBaseUrl}/{_settings.GeminiChatModel}:generateContent?key={_settings.GeminiApiKey}";
        var promptBuilder = new StringBuilder();
        promptBuilder.AppendLine("You are a helpful assistant. Use the provided context to answer in Arabic.");
        promptBuilder.AppendLine("Context:");
        foreach (var chunk in contextChunks)
        {
            promptBuilder.AppendLine("- " + chunk);
        }
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Question:");
        promptBuilder.AppendLine(question);

        var payload = new
        {
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = promptBuilder.ToString() } }
                }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        var candidates = doc.RootElement.GetProperty("candidates");
        if (candidates.GetArrayLength() == 0)
        {
            return "لم يتم العثور على إجابة.";
        }

        var text = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
        return text ?? "لم يتم العثور على إجابة.";
    }
}
