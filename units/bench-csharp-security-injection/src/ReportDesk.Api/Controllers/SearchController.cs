using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportDesk.Api.Documents;
using ReportDesk.Api.Search;
using ReportDesk.Api.Security;

namespace ReportDesk.Api.Controllers;

[ApiController]
[Route("search")]
[Authorize(Policy = AuthorizationPolicies.DocumentsRead)]
public sealed class SearchController(
    IDocumentSearchRepository search,
    IDocumentRepository documents,
    HighlightService highlighter) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<IReadOnlyList<DocumentHit>>> Search(SearchRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        Func<DocumentHit, bool> matches;
        try
        {
            matches = request.Filter is null ? _ => true : FilterTreeCompiler.Compile(request.Filter);
        }
        catch (FilterException ex)
        {
            return ValidationProblem(new ValidationProblemDetails { Detail = ex.Message });
        }

        var hits = await search.SearchAsync(request.Term, request.Limit, cancellationToken).ConfigureAwait(false);
        return Ok(hits.Where(matches).ToList());
    }

    [HttpGet("highlights")]
    public async Task<ActionResult<IReadOnlyList<Highlight>>> Highlights(
        [FromQuery, Required] Guid documentId,
        [FromQuery, Required, StringLength(200)] string pattern,
        CancellationToken cancellationToken)
    {
        var document = await documents.GetAsync(documentId, cancellationToken).ConfigureAwait(false);
        return document is null ? NotFound() : Ok(highlighter.FindAll(document.Body, pattern));
    }
}
