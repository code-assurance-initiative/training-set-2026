using System.DirectoryServices.Protocols;

namespace ReportDesk.Api.People;

public sealed record PersonEntry(string DistinguishedName, string DisplayName, string Department);

public sealed record GroupEntry(string DistinguishedName, string Name, IReadOnlyList<string> Members);

internal static class SearchResultEntryExtensions
{
    public static string Single(this SearchResultEntry entry, string attribute) =>
        entry.Attributes[attribute] is { Count: > 0 } values ? values[0]?.ToString() ?? string.Empty : string.Empty;

    public static IReadOnlyList<string> All(this SearchResultEntry entry, string attribute) =>
        entry.Attributes[attribute] is { } values
            ? values.GetValues(typeof(string)).Cast<string>().ToList()
            : [];
}
