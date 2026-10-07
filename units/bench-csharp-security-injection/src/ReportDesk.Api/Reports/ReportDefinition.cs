namespace ReportDesk.Api.Reports;

public sealed class ReportDefinition
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Owner { get; set; } = string.Empty;

    public string TemplateName { get; set; } = string.Empty;

    public string LayoutJson { get; set; } = "{}";

    public bool Archived { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
