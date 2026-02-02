using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using UglyToad.PdfPig;

namespace NotebookLMClone.Services;

public sealed class DocumentParser
{
    public async Task<IReadOnlyList<ParsedSegment>> ParseAsync(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => await ParsePdfAsync(filePath),
            ".docx" => await ParseDocxAsync(filePath),
            ".txt" => await ParseTextAsync(filePath),
            _ => Array.Empty<ParsedSegment>()
        };
    }

    private static Task<IReadOnlyList<ParsedSegment>> ParsePdfAsync(string filePath)
    {
        var results = new List<ParsedSegment>();
        using var document = PdfDocument.Open(filePath);
        foreach (var page in document.GetPages())
        {
            var text = page.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(text))
            {
                results.Add(new ParsedSegment(text, page.Number, 0));
            }
        }

        return Task.FromResult<IReadOnlyList<ParsedSegment>>(results);
    }

    private static Task<IReadOnlyList<ParsedSegment>> ParseDocxAsync(string filePath)
    {
        var results = new List<ParsedSegment>();
        using var document = WordprocessingDocument.Open(filePath, false);
        var paragraphs = document.MainDocumentPart?.Document.Body?.Elements<Paragraph>() ?? Enumerable.Empty<Paragraph>();
        var index = 0;
        foreach (var paragraph in paragraphs)
        {
            var text = paragraph.InnerText?.Trim();
            if (!string.IsNullOrWhiteSpace(text))
            {
                results.Add(new ParsedSegment(text, 0, index++));
            }
        }

        return Task.FromResult<IReadOnlyList<ParsedSegment>>(results);
    }

    private static async Task<IReadOnlyList<ParsedSegment>> ParseTextAsync(string filePath)
    {
        var content = await File.ReadAllTextAsync(filePath);
        var paragraphs = content.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
        var results = new List<ParsedSegment>();
        var index = 0;
        foreach (var paragraph in paragraphs)
        {
            var text = paragraph.Trim();
            if (!string.IsNullOrWhiteSpace(text))
            {
                results.Add(new ParsedSegment(text, 0, index++));
            }
        }

        return results;
    }
}

public sealed record ParsedSegment(string Text, int PageNumber, int ParagraphIndex);
