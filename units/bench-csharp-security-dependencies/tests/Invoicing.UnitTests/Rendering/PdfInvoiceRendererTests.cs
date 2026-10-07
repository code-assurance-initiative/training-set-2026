using System.Text;
using FluentAssertions;
using Invoicing.Rendering.Pdf;
using Invoicing.UnitTests.TestSupport;
using PdfSharpCore.Pdf.IO;

namespace Invoicing.UnitTests.Rendering;

public sealed class PdfInvoiceRendererTests
{
    [Fact]
    public void RendersAPdfDocument()
    {
        var pdf = Renderers.Pdf().Render(Invoices.Invoice(), InvoiceBranding.None);

        Encoding.ASCII.GetString(pdf, 0, 5).Should().Be("%PDF-");
        using var document = PdfReader.Open(new MemoryStream(pdf), PdfDocumentOpenMode.Import);
        document.PageCount.Should().Be(1);
        document.Info.Title.Should().Be("Invoice INV-2026-0042");
    }

    [Fact]
    public void RendersWithTheTenantLogoAndFooter()
    {
        var invoice = Invoices.Invoice();
        var plain = Renderers.Pdf().Render(invoice, InvoiceBranding.None);

        var branded = Renderers.Pdf().Render(invoice, new InvoiceBranding(Renderers.TinyPngLogo, "Registered in Aarhus"));

        branded.Length.Should().BeGreaterThan(plain.Length);
        Encoding.ASCII.GetString(branded).Should().Contain("/Subtype /Image");
    }
}
