namespace FleetOps.Infrastructure.Documents;

public static class CsvEscaping
{
    /// <summary>Quotes a field when it holds the separator, a quote or a line break (RFC 4180), and defuses
    /// spreadsheet formulas by prefixing a leading = + - @ with an apostrophe.</summary>
    public static string Field(string value, char separator)
    {
        ArgumentNullException.ThrowIfNull(value);
        var safe = value.Length > 0 && "=+-@".Contains(value[0], StringComparison.Ordinal) ? "'" + value : value;
        return safe.IndexOfAny([separator, '"', '\r', '\n']) >= 0
            ? "\"" + safe.Replace("\"", "\"\"", StringComparison.Ordinal) + "\""
            : safe;
    }
}
