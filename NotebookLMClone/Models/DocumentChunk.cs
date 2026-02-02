namespace NotebookLMClone.Models;

public sealed record DocumentChunk(
    string ProjectId,
    string DocumentId,
    string FileName,
    int PageNumber,
    int ParagraphIndex,
    string Text,
    float[] Embedding);
