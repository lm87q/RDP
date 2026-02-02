using System.Text.Json;
using NotebookLMClone.Models;

namespace NotebookLMClone.Services;

public sealed class ProjectRepository
{
    private readonly string _storagePath;

    public ProjectRepository()
    {
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NotebookLMClone");
        Directory.CreateDirectory(root);
        _storagePath = Path.Combine(root, "projects.json");
    }

    public async Task<IReadOnlyList<ProjectRecord>> LoadAsync()
    {
        if (!File.Exists(_storagePath))
        {
            return Array.Empty<ProjectRecord>();
        }

        var json = await File.ReadAllTextAsync(_storagePath);
        var projects = JsonSerializer.Deserialize<List<ProjectRecord>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return projects ?? Array.Empty<ProjectRecord>();
    }

    public async Task SaveAsync(IEnumerable<ProjectRecord> projects)
    {
        var json = JsonSerializer.Serialize(projects, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(_storagePath, json);
    }
}
