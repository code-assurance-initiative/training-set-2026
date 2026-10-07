using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportDesk.Api.Documents;
using ReportDesk.Api.Http;
using ReportDesk.Api.Metadata;
using ReportDesk.Api.Previews;
using ReportDesk.Api.Security;

namespace ReportDesk.Api.Controllers;

[ApiController]
[Route("documents")]
[Authorize(Policy = AuthorizationPolicies.DocumentsRead)]
public sealed class DocumentsController(IDocumentRepository documents, DocumentCardRenderer cards) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DocumentRecord>> Get(Guid id, CancellationToken cancellationToken)
    {
        var document = await documents.GetAsync(id, cancellationToken).ConfigureAwait(false);
        return document is null ? NotFound() : Ok(document);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DocumentRecord>>> List(
        [FromQuery, StringLength(20)] string? sort,
        [FromQuery] bool desc,
        [FromQuery, Range(1, 200)] int limit = 50,
        [FromQuery] string? number = null,
        CancellationToken cancellationToken = default)
    {
        if (number is not null && !DocumentNumber.IsValid(number))
        {
            return ValidationProblem(new ValidationProblemDetails { Detail = "Document numbers look like HR-2026-1042." });
        }

        var rows = await documents.ListAsync(sort, desc, limit, cancellationToken).ConfigureAwait(false);
        return Ok(number is null ? rows : rows.Where(row => row.Number == number).ToList());
    }

    [HttpGet("{id:guid}/card")]
    public async Task<IActionResult> Card(Guid id, CancellationToken cancellationToken)
    {
        var document = await documents.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (document is null)
        {
            return NotFound();
        }

        var html = cards.Render(SafeXml.LoadDocument(document.MetadataXml));
        Response.Headers.ETag = ETagCalculator.Compute(Encoding.UTF8.GetBytes(html));
        return Content(html, "text/html");
    }
}
