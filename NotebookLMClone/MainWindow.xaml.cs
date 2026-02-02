using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using Microsoft.Win32;
using NotebookLMClone.Models;
using NotebookLMClone.Services;
using NotebookLMClone.Services.VectorStore;

namespace NotebookLMClone;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<ProjectRecord> _projects = new();
    private readonly ProjectRepository _projectRepository = new();
    private readonly DocumentParser _documentParser = new();
    private readonly Chunker _chunker = new();
    private readonly GeminiClient _geminiClient;
    private readonly IVectorStore _vectorStore;
    private readonly AppSettings _settings;

    public MainWindow()
    {
        InitializeComponent();
        _settings = new AppSettings();
        _geminiClient = new GeminiClient(new HttpClient(), _settings);
        _vectorStore = string.IsNullOrWhiteSpace(_settings.ChromaEndpoint)
            ? new InMemoryVectorStore()
            : new ChromaVectorStore(new HttpClient(), _settings.ChromaEndpoint!);

        ProjectsListBox.ItemsSource = _projects;
        Loaded += MainWindow_OnLoaded;
    }

    private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        var existing = await _projectRepository.LoadAsync();
        foreach (var project in existing)
        {
            _projects.Add(project);
        }

        if (_projects.Count == 0)
        {
            CreateNewProject();
        }
        else
        {
            ProjectsListBox.SelectedIndex = 0;
        }
    }

    private void ProjectsListBox_OnSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ProjectsListBox.SelectedItem is ProjectRecord project)
        {
            FilesListBox.ItemsSource = project.Files;
            HistoryListBox.ItemsSource = project.History;
        }
    }

    private async void NewProjectButton_OnClick(object sender, RoutedEventArgs e)
    {
        CreateNewProject();
        await _projectRepository.SaveAsync(_projects);
    }

    private void CreateNewProject()
    {
        var projectNumber = _projects.Count + 1;
        var project = new ProjectRecord { Name = $"Project {projectNumber}" };
        _projects.Add(project);
        ProjectsListBox.SelectedItem = project;
    }

    private async void UploadFilesButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (ProjectsListBox.SelectedItem is not ProjectRecord project)
        {
            MessageBox.Show("Please select a project first.", "NotebookLM", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new OpenFileDialog
        {
            Filter = "Documents (*.pdf;*.docx;*.txt)|*.pdf;*.docx;*.txt",
            Multiselect = true
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        foreach (var file in dialog.FileNames)
        {
            var projectFile = new ProjectFile { FilePath = file };
            project.Files.Add(projectFile);

            var segments = await _documentParser.ParseAsync(file);
            var chunkInputs = _chunker.CreateChunks(segments);
            var documentChunks = new List<DocumentChunk>();

            foreach (var chunk in chunkInputs)
            {
                var embedding = await _geminiClient.CreateEmbeddingAsync(chunk.Text);
                documentChunks.Add(new DocumentChunk(
                    project.Id,
                    projectFile.Id,
                    projectFile.DisplayName,
                    chunk.PageNumber,
                    chunk.ParagraphIndex,
                    chunk.Text,
                    embedding));
            }

            if (documentChunks.Count > 0)
            {
                await _vectorStore.UpsertAsync(documentChunks);
            }
        }

        await _projectRepository.SaveAsync(_projects);
    }

    private async void AskButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (ProjectsListBox.SelectedItem is not ProjectRecord project)
        {
            MessageBox.Show("Please select a project first.", "NotebookLM", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var question = QuestionTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(question))
        {
            return;
        }

        var questionEmbedding = await _geminiClient.CreateEmbeddingAsync(question);
        var results = await _vectorStore.QueryAsync(project.Id, questionEmbedding, topK: 5);
        var sources = results
            .Select(result => new SourceCitation
            {
                SourceLabel = $"{result.Chunk.FileName} - Page {result.Chunk.PageNumber}",
                Snippet = Truncate(result.Chunk.Text)
            })
            .ToList();

        var contextText = results.Select(result => $"[{result.Chunk.FileName} p.{result.Chunk.PageNumber}] {result.Chunk.Text}");
        var answer = await _geminiClient.GenerateAnswerAsync(question, contextText);

        UpdateAnswerUi(answer, sources);

        project.History.Insert(0, new QuestionAnswerRecord
        {
            Question = question,
            Answer = answer,
            Sources = sources
        });

        await _projectRepository.SaveAsync(_projects);
    }

    private void UpdateAnswerUi(string answer, IReadOnlyList<SourceCitation> sources)
    {
        AnswerRichTextBox.Document.Blocks.Clear();
        var paragraph = new Paragraph();
        paragraph.Inlines.Add(new Run(answer));
        AnswerRichTextBox.Document.Blocks.Add(paragraph);

        SourcesDataGrid.ItemsSource = sources;
    }

    private static string Truncate(string text, int maxLength = 180)
    {
        if (text.Length <= maxLength)
        {
            return text;
        }

        return text.Substring(0, maxLength) + "...";
    }
}
