using System.Collections.ObjectModel;

namespace NotebookLMClone.Models;

public sealed class ProjectRecord
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "New Project";
    public ObservableCollection<ProjectFile> Files { get; } = new();
    public ObservableCollection<QuestionAnswerRecord> History { get; } = new();
}

public sealed class ProjectFile
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string FilePath { get; init; } = string.Empty;
    public string DisplayName => System.IO.Path.GetFileName(FilePath);
}

public sealed class QuestionAnswerRecord
{
    public string Question { get; init; } = string.Empty;
    public string Answer { get; init; } = string.Empty;
    public List<SourceCitation> Sources { get; init; } = new();
    public DateTimeOffset AskedAt { get; init; } = DateTimeOffset.Now;

    public string DisplayText => $"{AskedAt:yyyy-MM-dd HH:mm} · {Question}";
}

public sealed class SourceCitation
{
    public string SourceLabel { get; init; } = string.Empty;
    public string Snippet { get; init; } = string.Empty;
}
