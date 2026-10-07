using System.Text;
using System.Text.Json;
using ICSharpCode.SharpZipLib.Zip;

namespace Invoicing.ArchiveExporter;

/// <summary>
/// Packs a directory of archived invoices (<c>&lt;number&gt;.pdf</c> + <c>&lt;number&gt;.json</c>) into one ZIP:
/// every PDF stamped as an archive copy under <c>pdf/</c>, and an <c>index.csv</c> listing them.
/// </summary>
public sealed class ArchiveExporter
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly TextWriter _log;

    public ArchiveExporter(TextWriter log)
    {
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    public int Export(string sourceDirectory, Stream destination)
    {
        ArgumentNullException.ThrowIfNull(sourceDirectory);
        ArgumentNullException.ThrowIfNull(destination);

        var invoices = ReadInvoices(sourceDirectory);
        using var zip = new ZipOutputStream(destination) { IsStreamOwner = false };
        zip.SetLevel(6);

        foreach (var invoice in invoices)
        {
            var pdf = File.ReadAllBytes(Path.Combine(sourceDirectory, invoice.Number + ".pdf"));
            Add(zip, ArchiveLayout.PdfEntryName(invoice.Number), PdfArchiveStamper.Stamp(pdf));
        }

        using (var index = new MemoryStream())
        {
            using (var writer = new StreamWriter(index, new UTF8Encoding(false), 4096, leaveOpen: true))
            {
                ArchiveIndexWriter.Write(writer, invoices);
            }

            Add(zip, ArchiveLayout.IndexEntryName, index.ToArray());
        }

        zip.Finish();
        _log.WriteLine($"Exported {invoices.Count} invoice(s) from {sourceDirectory}");
        return invoices.Count;
    }

    private List<ArchivedInvoice> ReadInvoices(string sourceDirectory)
    {
        var invoices = new List<ArchivedInvoice>();
        foreach (var metadataPath in Directory.EnumerateFiles(sourceDirectory, "*.json").OrderBy(p => p, StringComparer.Ordinal))
        {
            var invoice = JsonSerializer.Deserialize<ArchivedInvoice>(File.ReadAllText(metadataPath), Json);
            if (invoice is null || !ArchiveLayout.IsValidInvoiceNumber(invoice.Number))
            {
                _log.WriteLine($"Skipped {Path.GetFileName(metadataPath)}: no valid invoice number");
                continue;
            }

            if (!File.Exists(Path.Combine(sourceDirectory, invoice.Number + ".pdf")))
            {
                _log.WriteLine($"Skipped {invoice.Number}: the PDF is missing");
                continue;
            }

            invoices.Add(invoice);
        }

        return invoices;
    }

    private static void Add(ZipOutputStream zip, string name, byte[] content)
    {
        zip.PutNextEntry(new ZipEntry(name) { DateTime = DateTime.UtcNow, Size = content.Length });
        zip.Write(content, 0, content.Length);
        zip.CloseEntry();
    }
}
