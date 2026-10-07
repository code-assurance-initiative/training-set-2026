namespace Invoicing.ArchiveExporter;

/// <summary>The metadata file the renderer wrote next to each archived PDF (<c>&lt;number&gt;.json</c>).</summary>
public sealed class ArchivedInvoice
{
    public string Number { get; set; } = string.Empty;

    public string Customer { get; set; } = string.Empty;

    public DateTime IssueDate { get; set; }

    public decimal GrossTotal { get; set; }

    public string Currency { get; set; } = string.Empty;
}
