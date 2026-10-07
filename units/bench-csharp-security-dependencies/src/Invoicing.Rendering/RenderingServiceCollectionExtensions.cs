using Invoicing.Rendering.Pdf;
using Invoicing.Rendering.Ubl;
using Microsoft.Extensions.DependencyInjection;

namespace Invoicing.Rendering;

public static class RenderingServiceCollectionExtensions
{
    /// <summary>Registers the renderers. The host registers its own <see cref="ISigningCertificateSource"/>.</summary>
    public static IServiceCollection AddInvoiceRendering(this IServiceCollection services, Action<PdfRenderingOptions>? configure = null)
    {
        var options = new PdfRenderingOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);
        services.AddSingleton<PdfInvoiceRenderer>();
        services.AddSingleton<XmlInvoiceSigner>();
        return services;
    }
}
