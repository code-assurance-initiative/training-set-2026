using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace Invoicing.ArchiveExporter;

/// <summary>Writes <c>index.csv</c>: one row per archived invoice, invariant culture, header row first.</summary>
public static class ArchiveIndexWriter
{
    public static void Write(TextWriter writer, IEnumerable<ArchivedInvoice> invoices)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(invoices);

        var configuration = new Configuration { CultureInfo = CultureInfo.InvariantCulture, Delimiter = "," };
        using var csv = new CsvWriter(writer, configuration, leaveOpen: true);
        csv.WriteRecords(invoices.Select(invoice => new IndexRow
        {
            Number = invoice.Number,
            Customer = invoice.Customer,
            IssueDate = invoice.IssueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            GrossTotal = invoice.GrossTotal,
            Currency = invoice.Currency,
            File = ArchiveLayout.PdfEntryName(invoice.Number),
        }));
    }

    private sealed class IndexRow
    {
        public string Number { get; set; } = string.Empty;

        public string Customer { get; set; } = string.Empty;

        public string IssueDate { get; set; } = string.Empty;

        public decimal GrossTotal { get; set; }

        public string Currency { get; set; } = string.Empty;

        public string File { get; set; } = string.Empty;
    }
}
