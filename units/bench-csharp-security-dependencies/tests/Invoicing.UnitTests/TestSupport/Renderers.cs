using Invoicing.Rendering.Pdf;
using Microsoft.Extensions.Logging.Abstractions;

namespace Invoicing.UnitTests.TestSupport;

public static class Renderers
{
    public static PdfInvoiceRenderer Pdf() => new(new PdfRenderingOptions(), NullLogger<PdfInvoiceRenderer>.Instance);

    /// <summary>A 1x1 PNG, the smallest logo a tenant can upload.</summary>
    public static byte[] TinyPngLogo { get; } = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");
}
