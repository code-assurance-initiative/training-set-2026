using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportDesk.Api.Security;
using ReportDesk.Api.Webhooks;

namespace ReportDesk.Api.Controllers;

[ApiController]
[Route("webhooks")]
[Authorize(Policy = AuthorizationPolicies.ReportsWrite)]
public sealed class WebhooksController(WebhookDispatcher dispatcher, TimeProvider clock) : ControllerBase
{
    /// <summary>Sends a test event to a callback before the tenant registers it.</summary>
    [HttpPost("test")]
    public async Task<ActionResult<bool>> Test(WebhookTestRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.CallbackUrl is not { IsAbsoluteUri: true, Scheme: "https" } callback)
        {
            return ValidationProblem(new ValidationProblemDetails { Detail = "Callbacks must be absolute https URLs." });
        }

        var ping = new ReportReadyEvent(Guid.Empty, "webhook test", clock.GetUtcNow());
        return Ok(await dispatcher.NotifyAsync(callback, ping, cancellationToken).ConfigureAwait(false));
    }
}
