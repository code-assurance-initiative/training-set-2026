using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportDesk.Api.Feeds;
using ReportDesk.Api.Security;

namespace ReportDesk.Api.Controllers;

[ApiController]
[Route("feeds")]
[Authorize(Policy = AuthorizationPolicies.Administer)]
public sealed class FeedsController(PartnerFeedClient feeds) : ControllerBase
{
    [HttpPost("pull")]
    public async Task<ActionResult<int>> Pull(FeedPullRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            var feed = await feeds.FetchAsync(request.FeedUrl, cancellationToken).ConfigureAwait(false);
            return Ok(feed.Length);
        }
        catch (FeedNotAllowedException ex)
        {
            return ValidationProblem(new ValidationProblemDetails { Detail = ex.Message });
        }
    }
}
