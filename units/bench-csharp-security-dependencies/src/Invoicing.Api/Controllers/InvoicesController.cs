using System.Text;
using Invoicing.Api.Branding;
using Invoicing.Api.Requests;
using Invoicing.Api.Security;
using Invoicing.Rendering.Pdf;
using Invoicing.Rendering.Ubl;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Invoicing.Api.Controllers;

[ApiController]
[Route("invoices")]
[Authorize(Policy = AuthorizationPolicies.RenderInvoices)]
public sealed class InvoicesController(
    PdfInvoiceRenderer pdf,
    XmlInvoiceSigner signer,
    ILogoSource logos,
    IOptions<BrandingOptions> branding) : ControllerBase
{
    /// <summary>Renders the invoice as a PDF with the caller's tenant logo.</summary>
    [HttpPost("pdf")]
    [Produces("application/pdf")]
    public async Task<IActionResult> RenderPdf([FromBody] RenderInvoiceRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var tenant = TenantClaims.TenantOf(User);
        if (tenant is null)
        {
            return Problem(statusCode: StatusCodes.Status403Forbidden, title: "The access token names no valid tenant.");
        }

        var logo = await logos.GetLogoAsync(tenant, cancellationToken).ConfigureAwait(false);
        var bytes = pdf.Render(request.ToDocument(), new InvoiceBranding(logo, branding.Value.FooterText));
        return File(bytes, "application/pdf", $"{request.Number}.pdf");
    }

    /// <summary>Renders the invoice as a signed UBL 2.1 e-invoice.</summary>
    [HttpPost("ubl")]
    [Produces("application/xml")]
    public IActionResult RenderUbl([FromBody] RenderInvoiceRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var signed = signer.Sign(UblInvoiceWriter.Write(request.ToDocument()));
        return File(Encoding.UTF8.GetBytes(signed.OuterXml), "application/xml", $"{request.Number}.xml");
    }
}
