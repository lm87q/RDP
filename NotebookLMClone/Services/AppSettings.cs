namespace NotebookLMClone.Services;

public sealed class AppSettings
{
    public string GeminiApiKey { get; init; } = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? string.Empty;
    public string GeminiEmbeddingModel { get; init; } = Environment.GetEnvironmentVariable("GEMINI_EMBEDDING_MODEL") ?? "models/embedding-001";
    public string GeminiChatModel { get; init; } = Environment.GetEnvironmentVariable("GEMINI_CHAT_MODEL") ?? "models/gemini-1.5-flash";
    public string GeminiBaseUrl { get; init; } = Environment.GetEnvironmentVariable("GEMINI_BASE_URL") ?? "https://generativelanguage.googleapis.com/v1beta";
    public string? ChromaEndpoint { get; init; } = Environment.GetEnvironmentVariable("CHROMA_ENDPOINT");
}
