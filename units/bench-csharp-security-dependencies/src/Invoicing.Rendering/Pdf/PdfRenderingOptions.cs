namespace Invoicing.Rendering.Pdf;

public sealed class PdfRenderingOptions
{
    /// <summary>A font family installed on the host; the container image ships DejaVu.</summary>
    public string FontFamily { get; set; } = "DejaVu Sans";

    /// <summary>Widest logo, in points, before it is scaled down.</summary>
    public double MaxLogoWidth { get; set; } = 140;
}
