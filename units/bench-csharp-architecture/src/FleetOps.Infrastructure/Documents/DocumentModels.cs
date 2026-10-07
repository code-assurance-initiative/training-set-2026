namespace FleetOps.Infrastructure.Documents;

public enum ExportFormat
{
    Csv,
    Tsv,
}

public sealed record CsvColumn<T>(string Header, Func<T, string> Value);

public sealed record ExportedDocument(string FileName, string ContentType, string Content);

public sealed class CsvExportOptions
{
    public ExportFormat Format { get; set; } = ExportFormat.Csv;

    public bool IncludeHeader { get; set; } = true;
}

public sealed class DocumentOptions
{
    public const string SectionName = "Documents";

    public string FilePrefix { get; set; } = "fleetops";
}
