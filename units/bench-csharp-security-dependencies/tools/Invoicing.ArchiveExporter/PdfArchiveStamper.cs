using iTextSharp.text;
using iTextSharp.text.pdf;

namespace Invoicing.ArchiveExporter;

/// <summary>Stamps "ARCHIVE COPY" diagonally across every page, so an archived invoice is never mistaken for the original.</summary>
public static class PdfArchiveStamper
{
    public static byte[] Stamp(byte[] pdf)
    {
        ArgumentNullException.ThrowIfNull(pdf);

        var reader = new PdfReader(pdf);
        try
        {
            using var output = new MemoryStream();
            using (var stamper = new PdfStamper(reader, output))
            {
                StampPages(reader, stamper);
            }

            return output.ToArray();
        }
        finally
        {
            reader.Close();
        }
    }

    private static void StampPages(PdfReader reader, PdfStamper stamper)
    {
        var font = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.WINANSI, BaseFont.NOT_EMBEDDED);
        for (var page = 1; page <= reader.NumberOfPages; page++)
        {
            var size = reader.GetPageSize(page);
            var canvas = stamper.GetOverContent(page);
            canvas.SaveState();
            canvas.SetGState(new PdfGState { FillOpacity = 0.25f });
            canvas.BeginText();
            canvas.SetFontAndSize(font, 48);
            canvas.ShowTextAligned(Element.ALIGN_CENTER, "ARCHIVE COPY", size.Width / 2, size.Height / 2, 45);
            canvas.EndText();
            canvas.RestoreState();
        }
    }
}
