using System.Text;
using System.Text.Json;
using FluentAssertions;
using Invoicing.Rendering.Pdf;
using Invoicing.UnitTests.TestSupport;
using SharpCompress.Archives.Zip;

namespace Invoicing.UnitTests.Archive;

public sealed class ArchiveExporterTests : IDisposable
{
    private readonly DirectoryInfo _archive = Directory.CreateTempSubdirectory("invoice-archive-");

    [Fact]
    public void PacksStampedPdfsAndAnIndex()
    {
        Archive("INV-2025-0001", 1250.00m);
        Archive("INV-2025-0002", 99.95m);
        using var zip = new MemoryStream();

        var count = new ArchiveExporter.ArchiveExporter(TextWriter.Null).Export(_archive.FullName, zip);

        count.Should().Be(2);
        zip.Position = 0;
        using var archive = ZipArchive.Open(zip);
        archive.Entries.Select(e => e.Key).Should().BeEquivalentTo("pdf/INV-2025-0001.pdf", "pdf/INV-2025-0002.pdf", "index.csv");
        var index = Read(archive, "index.csv");
        index.Split('\n', StringSplitOptions.RemoveEmptyEntries).Should().HaveCount(3);
        index.Should().StartWith("Number,Customer,IssueDate,GrossTotal,Currency,File").And.Contain("INV-2025-0002,Fjord Logistics A/S,2025-03-31,99.95,EUR,pdf/INV-2025-0002.pdf");
    }

    [Fact]
    public void AStampedCopyIsALargerPdfThanTheOriginal()
    {
        var original = Archive("INV-2025-0003", 10m);
        using var zip = new MemoryStream();

        new ArchiveExporter.ArchiveExporter(TextWriter.Null).Export(_archive.FullName, zip);

        zip.Position = 0;
        using var archive = ZipArchive.Open(zip);
        var stamped = ReadBytes(archive, "pdf/INV-2025-0003.pdf");
        Encoding.ASCII.GetString(stamped, 0, 5).Should().Be("%PDF-");
        stamped.Length.Should().BeGreaterThan(original.Length);
    }

    [Fact]
    public void SkipsMetadataWithoutAValidNumberOrWithoutItsPdf()
    {
        Archive("INV-2025-0004", 10m);
        File.WriteAllText(Path.Combine(_archive.FullName, "broken.json"), """{ "number": "../escape" }""");
        File.WriteAllText(Path.Combine(_archive.FullName, "INV-2025-0005.json"), """{ "number": "INV-2025-0005" }""");
        var log = new StringWriter();
        using var zip = new MemoryStream();

        var count = new ArchiveExporter.ArchiveExporter(log).Export(_archive.FullName, zip);

        count.Should().Be(1);
        log.ToString().Should().Contain("Skipped broken.json").And.Contain("Skipped INV-2025-0005: the PDF is missing");
    }

    public void Dispose() => _archive.Delete(recursive: true);

    private byte[] Archive(string number, decimal gross)
    {
        var pdf = Renderers.Pdf().Render(Invoices.Invoice(number), InvoiceBranding.None);
        File.WriteAllBytes(Path.Combine(_archive.FullName, number + ".pdf"), pdf);
        var metadata = new { number, customer = "Fjord Logistics A/S", issueDate = "2025-03-31", grossTotal = gross, currency = "EUR" };
        File.WriteAllText(Path.Combine(_archive.FullName, number + ".json"), JsonSerializer.Serialize(metadata));
        return pdf;
    }

    private static byte[] ReadBytes(ZipArchive archive, string key)
    {
        using var entry = archive.Entries.Single(e => e.Key == key).OpenEntryStream();
        using var copy = new MemoryStream();
        entry.CopyTo(copy);
        return copy.ToArray();
    }

    private static string Read(ZipArchive archive, string key) => Encoding.UTF8.GetString(ReadBytes(archive, key));
}
