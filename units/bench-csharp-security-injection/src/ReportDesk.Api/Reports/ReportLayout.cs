namespace ReportDesk.Api.Reports;

/// <summary>How a report is laid out: page settings and an ordered list of sections.</summary>
public sealed class ReportLayout
{
    public string PageSize { get; set; } = "A4";

    public bool Landscape { get; set; }

    public List<LayoutSection> Sections { get; set; } = [];
}

public sealed class LayoutSection
{
    public string Kind { get; set; } = "table";

    public string Heading { get; set; } = string.Empty;

    public List<string> Columns { get; set; } = [];
}
