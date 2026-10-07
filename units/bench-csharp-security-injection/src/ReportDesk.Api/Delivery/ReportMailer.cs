using ReportDesk.Api.Reports;

namespace ReportDesk.Api.Delivery;

/// <summary>Mails a rendered report to one subscriber and records bounces.</summary>
public sealed partial class ReportMailer(IMailTransport transport, EmailPseudonymizer pseudonymizer, ILogger<ReportMailer> logger)
{
    public async Task DeliverAsync(
        ReportDefinition report, Subscriber subscriber, byte[] rendered, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(subscriber);
        LogDelivering(report.Id, subscriber.EmailAddress);

        var message = new MailMessageRequest(
            subscriber.EmailAddress,
            $"Report: {report.Name}",
            $"Hello {subscriber.DisplayName},\n\nyour scheduled report \"{report.Name}\" is attached.",
            $"{report.Name}.pdf",
            rendered);
        await transport.SendAsync(message, cancellationToken).ConfigureAwait(false);
    }

    public void RecordBounce(Subscriber subscriber, string reason)
    {
        ArgumentNullException.ThrowIfNull(subscriber);
        var emailAddressHash = pseudonymizer.Hash(subscriber.EmailAddress);
        LogBounce(subscriber.Id, emailAddressHash, reason);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Delivering report {ReportId} to {EmailAddress}")]
    private partial void LogDelivering(Guid reportId, string emailAddress);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Delivery to subscriber {SubscriberId} bounced for {EmailAddressHash}: {Reason}")]
    private partial void LogBounce(Guid subscriberId, string emailAddressHash, string reason);
}
