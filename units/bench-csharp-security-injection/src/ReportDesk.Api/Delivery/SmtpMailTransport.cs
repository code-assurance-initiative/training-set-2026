using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace ReportDesk.Api.Delivery;

/// <summary>Sends report mails through the platform's internal relay.</summary>
public sealed class SmtpMailTransport(IOptions<DeliveryOptions> options) : IMailTransport
{
    public async Task SendAsync(MailMessageRequest message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        var settings = options.Value;
        using var client = new SmtpClient(settings.SmtpHost) { EnableSsl = true };
        using var mail = new MailMessage(settings.SenderAddress, message.To, message.Subject, message.Body);
        using var attachmentStream = new MemoryStream(message.Attachment);
        mail.Attachments.Add(new Attachment(attachmentStream, message.AttachmentName));
        await client.SendMailAsync(mail, cancellationToken).ConfigureAwait(false);
    }
}
