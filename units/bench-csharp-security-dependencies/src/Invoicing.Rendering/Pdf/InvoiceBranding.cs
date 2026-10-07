namespace Invoicing.Rendering.Pdf;

/// <summary>Per-tenant decoration. The logo is the image file the tenant uploaded (PNG or JPEG).</summary>
public sealed record InvoiceBranding(byte[]? Logo, string FooterText)
{
    public static InvoiceBranding None { get; } = new(null, string.Empty);
}
