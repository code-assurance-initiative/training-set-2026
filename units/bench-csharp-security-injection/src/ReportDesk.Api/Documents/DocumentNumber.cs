using System.Text.RegularExpressions;

namespace ReportDesk.Api.Documents;

/// <summary>Archive document numbers: a register prefix, the year and a sequence, e.g. <c>HR-2026-1042</c>.</summary>
public static class DocumentNumber
{
    private static readonly Regex Format =
        new(@"^[A-Z]{2,5}-\d{4}-\d{1,6}$", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

    public static bool IsValid(string? value) => value is not null && Format.IsMatch(value);
}
