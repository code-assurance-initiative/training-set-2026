using System.Text;

namespace FleetOps.Infrastructure.Documents;

public static class CsvDocument
{
    public static string Write<T>(IEnumerable<T> rows, IReadOnlyList<CsvColumn<T>> columns, CsvExportOptions options)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentNullException.ThrowIfNull(options);
        var separator = options.Format == ExportFormat.Tsv ? '\t' : ',';
        var text = new StringBuilder();
        if (options.IncludeHeader)
        {
            text.AppendJoin(separator, columns.Select(c => CsvEscaping.Field(c.Header, separator))).Append("\r\n");
        }

        foreach (var row in rows)
        {
            text.AppendJoin(separator, columns.Select(c => CsvEscaping.Field(c.Value(row), separator))).Append("\r\n");
        }

        return text.ToString();
    }
}
