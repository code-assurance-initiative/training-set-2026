namespace Invoicing.ArchiveExporter;

/// <summary>Entry names inside the archive ZIP. Invoice numbers are restricted to [A-Z0-9-] when they are issued.</summary>
public static class ArchiveLayout
{
    public const string IndexEntryName = "index.csv";

    public static string PdfEntryName(string invoiceNumber) => $"pdf/{invoiceNumber}.pdf";

    public static bool IsValidInvoiceNumber(string number) =>
        number.Length is > 0 and <= 32 && number.All(c => c is (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '-');
}
