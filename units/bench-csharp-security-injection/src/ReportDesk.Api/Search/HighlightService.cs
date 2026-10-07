using System.Text.RegularExpressions;

namespace ReportDesk.Api.Search;

/// <summary>Finds the spans of a document's text that a reader asked to have highlighted.</summary>
public sealed class HighlightService
{
    public const int MaxHighlights = 200;

    public IReadOnlyList<Highlight> FindAll(string text, string pattern)
    {
        ArgumentNullException.ThrowIfNull(text);
        var regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
        var highlights = new List<Highlight>();
        for (var match = regex.Match(text); match.Success && highlights.Count < MaxHighlights; match = match.NextMatch())
        {
            highlights.Add(new Highlight(match.Index, match.Length));
        }

        return highlights;
    }
}

public sealed record Highlight(int Start, int Length);
