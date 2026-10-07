using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportDesk.Api.Search;
using ReportDesk.Api.Security;

namespace ReportDesk.Api.Controllers;

/// <summary>HTML fragments for the archive front end's quick-search box, which inserts them into its page.</summary>
[ApiController]
[Route("widgets/search")]
[Authorize(Policy = AuthorizationPolicies.DocumentsRead)]
public sealed class SearchWidgetController(IDocumentSearchRepository search) : ControllerBase
{
    private const int WidgetSize = 8;

    [HttpGet]
    public async Task<ContentResult> Render([FromQuery, Required, StringLength(200)] string q, CancellationToken cancellationToken)
    {
        var hits = await search.SearchAsync(q, WidgetSize, cancellationToken).ConfigureAwait(false);
        if (hits.Count == 0)
        {
            return Content($"<p class=\"quick-search-empty\">No documents match <strong>{q}</strong>.</p>", "text/html");
        }

        return Content(RenderHits(hits), "text/html");
    }

    private static string RenderHits(IReadOnlyList<DocumentHit> hits)
    {
        var html = new StringBuilder("<ul class=\"quick-search\">");
        foreach (var hit in hits)
        {
            var title = HtmlEncoder.Default.Encode(hit.Title);
            html.Append("<li><a href=\"/documents/").Append(hit.Id.ToString("D")).Append("\">").Append(title).Append("</a></li>");
        }

        return html.Append("</ul>").ToString();
    }
}
