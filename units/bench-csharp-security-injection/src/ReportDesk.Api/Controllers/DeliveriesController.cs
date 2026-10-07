using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportDesk.Api.Delivery;
using ReportDesk.Api.Reports;
using ReportDesk.Api.Security;

namespace ReportDesk.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.ReportsWrite)]
public sealed class DeliveriesController(
    IReportRepository reports,
    ISubscriberStore subscribers,
    SubscriberDirectoryClient crm,
    ReportMailer mailer) : ControllerBase
{
    [HttpPost("deliveries")]
    public async Task<IActionResult> Deliver(DeliveryRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var report = await reports.GetAsync(request.ReportId, cancellationToken).ConfigureAwait(false);
        var subscriber = await subscribers.GetAsync(request.SubscriberId, cancellationToken).ConfigureAwait(false);
        if (report is null || subscriber is null)
        {
            return NotFound();
        }

        if (!subscriber.EmailEnabled)
        {
            return Conflict();
        }

        var contact = await crm.FindContactAsync(subscriber.EmailAddress, cancellationToken).ConfigureAwait(false);
        if (contact is not null)
        {
            subscriber.DisplayName = contact.DisplayName;
        }

        await mailer.DeliverAsync(report, subscriber, [], cancellationToken).ConfigureAwait(false);
        return Accepted();
    }

    [HttpPost("deliveries/bounces")]
    public async Task<IActionResult> Bounce(BounceRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var subscriber = await subscribers.GetAsync(request.SubscriberId, cancellationToken).ConfigureAwait(false);
        if (subscriber is null)
        {
            return NotFound();
        }

        mailer.RecordBounce(subscriber, request.Reason);
        return NoContent();
    }

    [HttpPut("subscriptions/{id:guid}/channels/email")]
    public async Task<IActionResult> SetEmailChannel(Guid id, EmailChannelRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        await subscribers.SetEmailEnabledAsync(id, request.Enabled, cancellationToken).ConfigureAwait(false);
        await crm.SetEmailChannelAsync(id, request.Enabled, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }
}
