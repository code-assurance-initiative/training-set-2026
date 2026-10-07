using System.Globalization;
using Invoicing.Contracts;
using Microsoft.Extensions.Logging;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace Invoicing.Rendering.Pdf;

/// <summary>Lays out one invoice on A4 pages: header and logo, the two parties, the lines, the totals.</summary>
public sealed partial class PdfInvoiceRenderer(PdfRenderingOptions options, ILogger<PdfInvoiceRenderer> logger)
{
    private const double Margin = 50;
    private const double LineHeight = 16;
    private static readonly CultureInfo Money = CultureInfo.InvariantCulture;

    public byte[] Render(InvoiceDocument invoice, InvoiceBranding branding)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        ArgumentNullException.ThrowIfNull(branding);

        using var document = new PdfDocument();
        document.Info.Title = $"Invoice {invoice.Number}";
        var page = document.AddPage();
        page.Size = PdfSharpCore.PageSize.A4;

        using (var graphics = XGraphics.FromPdfPage(page))
        {
            var fonts = new Fonts(options.FontFamily);
            var y = DrawHeader(graphics, fonts, invoice, branding);
            y = DrawParties(graphics, fonts, invoice, y);
            y = DrawLines(graphics, fonts, invoice, y);
            DrawTotals(graphics, fonts, invoice, y);
            DrawFooter(graphics, fonts, page, branding);
        }

        using var output = new MemoryStream();
        document.Save(output, closeStream: false);
        LogRendered(invoice.Number, invoice.Lines.Count, output.Length);
        return output.ToArray();
    }

    private double DrawHeader(XGraphics graphics, Fonts fonts, InvoiceDocument invoice, InvoiceBranding branding)
    {
        if (branding.Logo is { Length: > 0 } logo)
        {
            using var image = XImage.FromStream(() => new MemoryStream(logo, writable: false));
            var scale = Math.Min(1.0, options.MaxLogoWidth / image.PointWidth);
            graphics.DrawImage(image, Margin, Margin, image.PointWidth * scale, image.PointHeight * scale);
        }

        var right = graphics.PageSize.Width - Margin;
        graphics.DrawString($"Invoice {invoice.Number}", fonts.Title, XBrushes.Black, new XRect(Margin, Margin, right - Margin, 24), XStringFormats.TopRight);
        graphics.DrawString($"Issued {invoice.IssueDate:yyyy-MM-dd} · due {invoice.DueDate:yyyy-MM-dd}", fonts.Body, XBrushes.Black, new XRect(Margin, Margin + 28, right - Margin, LineHeight), XStringFormats.TopRight);
        return Margin + 90;
    }

    private static double DrawParties(XGraphics graphics, Fonts fonts, InvoiceDocument invoice, double top)
    {
        var half = (graphics.PageSize.Width - (2 * Margin)) / 2;
        var sellerBottom = DrawParty(graphics, fonts, "From", invoice.Seller, Margin, top);
        var buyerBottom = DrawParty(graphics, fonts, "To", invoice.Buyer, Margin + half, top);
        return Math.Max(sellerBottom, buyerBottom) + LineHeight;
    }

    private static double DrawParty(XGraphics graphics, Fonts fonts, string caption, Party party, double x, double y)
    {
        graphics.DrawString(caption, fonts.Bold, XBrushes.Black, x, y);
        y += LineHeight;
        foreach (var text in new[] { party.Name }.Concat(party.AddressLines).Append(party.CountryCode))
        {
            graphics.DrawString(text, fonts.Body, XBrushes.Black, x, y);
            y += LineHeight;
        }

        if (party.VatId.Length > 0)
        {
            graphics.DrawString($"VAT {party.VatId}", fonts.Body, XBrushes.Black, x, y);
            y += LineHeight;
        }

        return y;
    }

    private static double DrawLines(XGraphics graphics, Fonts fonts, InvoiceDocument invoice, double y)
    {
        var right = graphics.PageSize.Width - Margin;
        graphics.DrawString("Description", fonts.Bold, XBrushes.Black, Margin, y);
        DrawAmount(graphics, fonts.Bold, "Qty", right - 220, y);
        DrawAmount(graphics, fonts.Bold, "Unit price", right - 110, y);
        DrawAmount(graphics, fonts.Bold, "Net", right, y);
        y += 4;
        graphics.DrawLine(XPens.Black, Margin, y, right, y);
        y += LineHeight;

        foreach (var line in invoice.Lines)
        {
            graphics.DrawString(line.Description, fonts.Body, XBrushes.Black, Margin, y);
            DrawAmount(graphics, fonts.Body, line.Quantity.ToString("0.##", Money), right - 220, y);
            DrawAmount(graphics, fonts.Body, line.UnitPrice.ToString("N2", Money), right - 110, y);
            DrawAmount(graphics, fonts.Body, line.NetAmount.ToString("N2", Money), right, y);
            y += LineHeight;
        }

        return y + LineHeight;
    }

    private static void DrawTotals(XGraphics graphics, Fonts fonts, InvoiceDocument invoice, double y)
    {
        var right = graphics.PageSize.Width - Margin;
        foreach (var (caption, amount, font) in new[]
                 {
                     ("Net total", invoice.NetTotal, fonts.Body),
                     ("VAT", invoice.VatTotal, fonts.Body),
                     ($"Total {invoice.Currency}", invoice.GrossTotal, fonts.Bold),
                 })
        {
            DrawAmount(graphics, font, caption, right - 110, y);
            DrawAmount(graphics, font, amount.ToString("N2", Money), right, y);
            y += LineHeight;
        }

        graphics.DrawString($"Payment reference: {invoice.PaymentReference}", fonts.Body, XBrushes.Black, Margin, y + LineHeight);
    }

    private static void DrawFooter(XGraphics graphics, Fonts fonts, PdfPage page, InvoiceBranding branding)
    {
        if (branding.FooterText.Length > 0)
        {
            graphics.DrawString(branding.FooterText, fonts.Small, XBrushes.Gray, new XRect(Margin, page.Height - Margin, page.Width - (2 * Margin), LineHeight), XStringFormats.Center);
        }
    }

    private static void DrawAmount(XGraphics graphics, XFont font, string text, double right, double y) =>
        graphics.DrawString(text, font, XBrushes.Black, new XRect(right - 100, y - font.Height + 2, 100, font.Height), XStringFormats.TopRight);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Rendered invoice {Number} ({LineCount} lines) to {Bytes} bytes of PDF")]
    private partial void LogRendered(string number, int lineCount, long bytes);

    private sealed class Fonts(string family)
    {
        public XFont Title { get; } = new(family, 18, XFontStyle.Bold);

        public XFont Bold { get; } = new(family, 10, XFontStyle.Bold);

        public XFont Body { get; } = new(family, 10, XFontStyle.Regular);

        public XFont Small { get; } = new(family, 8, XFontStyle.Regular);
    }
}
