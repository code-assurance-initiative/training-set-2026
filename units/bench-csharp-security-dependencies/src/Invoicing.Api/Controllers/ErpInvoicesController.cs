using Invoicing.Api.Erp;
using Invoicing.Api.Security;
using Invoicing.Rendering.Pdf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Invoicing.Api.Controllers;

/// <summary>The webhook the customers' ERP connector calls with its native invoice payload.</summary>
[ApiController]
[Route("erp/invoices")]
[Authorize(Policy = AuthorizationPolicies.SubmitErpInvoices)]
public sealed partial class ErpInvoicesController(PdfInvoiceRenderer pdf, ILogger<ErpInvoicesController> logger) : ControllerBase
{
    private const int MaxPayloadBytes = 256 * 1024;

    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/pdf")]
    [RequestSizeLimit(MaxPayloadBytes)]
    public async Task<IActionResult> Submit(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var json = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var invoice = ErpWebhookParser.Parse(json);
            LogAccepted(invoice.Number);
            return File(pdf.Render(invoice, InvoiceBranding.None), "application/pdf", $"{invoice.Number}.pdf");
        }
        catch (ErpPayloadException ex)
        {
            LogRejected(ex.Message);
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "The ERP payload was rejected.", detail: ex.Message);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Accepted ERP invoice {Number}")]
    private partial void LogAccepted(string number);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Rejected ERP payload: {Reason}")]
    private partial void LogRejected(string reason);
}
