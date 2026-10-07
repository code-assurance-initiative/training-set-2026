using System.Globalization;
using Invoicing.Contracts;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Invoicing.Worker.Mail;

/// <summary>Sends the rendered invoice as a PDF attachment over STARTTLS.</summary>
public sealed partial class SmtpInvoiceMailer(IOptions<SmtpOptions> options, ILogger<SmtpInvoiceMailer> logger) : IInvoiceMailer
{
    public async Task SendAsync(string recipient, InvoiceDocument invoice, byte[] pdf, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        var message = Compose(options.Value, recipient, invoice, pdf);

        using var client = new SmtpClient();
        var settings = options.Value;
        await client.ConnectAsync(settings.Host, settings.Port, SecureSocketOptions.StartTls, cancellationToken).ConfigureAwait(false);
        if (settings.UserName.Length > 0)
        {
            await client.AuthenticateAsync(settings.UserName, settings.Password, cancellationToken).ConfigureAwait(false);
        }

        await client.SendAsync(message, cancellationToken).ConfigureAwait(false);
        await client.DisconnectAsync(quit: true, cancellationToken).ConfigureAwait(false);
        LogSent(invoice.Number);
    }

    public static MimeMessage Compose(SmtpOptions settings, string recipient, InvoiceDocument invoice, byte[] pdf)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(invoice);

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(settings.FromName, settings.FromAddress));
        message.To.Add(MailboxAddress.Parse(recipient));
        message.Subject = $"Invoice {invoice.Number} from {invoice.Seller.Name}";

        var body = new BodyBuilder
        {
            TextBody = string.Create(
                CultureInfo.InvariantCulture,
                $"Please find invoice {invoice.Number} attached: {invoice.GrossTotal:N2} {invoice.Currency}, due {invoice.DueDate:yyyy-MM-dd}.\nPayment reference: {invoice.PaymentReference}"),
        };
        body.Attachments.Add($"{invoice.Number}.pdf", pdf, new ContentType("application", "pdf"));
        message.Body = body.ToMessageBody();
        return message;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "E-mailed invoice {Number}")]
    private partial void LogSent(string number);
}
