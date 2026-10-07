using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FleetOps.Infrastructure.Email;

/// <summary>Writes each message as an RFC 5322 file into the relay's pickup directory.</summary>
public sealed partial class PickupDirectoryEmailSender(
    IOptions<EmailOptions> options,
    TimeProvider clock,
    ILogger<PickupDirectoryEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        var settings = options.Value;
        var file = Path.Combine(settings.PickupDirectory, $"{Guid.NewGuid():N}.eml");
        var content = new StringBuilder()
            .Append(CultureInfo.InvariantCulture, $"From: {settings.From}\r\n")
            .Append(CultureInfo.InvariantCulture, $"To: {message.To}\r\n")
            .Append(CultureInfo.InvariantCulture, $"Date: {clock.GetUtcNow():R}\r\n")
            .Append(CultureInfo.InvariantCulture, $"Subject: {message.Subject}\r\n")
            .Append(message.Priority == EmailPriority.High ? "X-Priority: 1\r\n" : string.Empty)
            .Append("\r\n")
            .Append(message.Body)
            .ToString();
        try
        {
            Directory.CreateDirectory(settings.PickupDirectory);
            await File.WriteAllTextAsync(file, content, cancellationToken).ConfigureAwait(false);
        }
        catch (IOException ex)
        {
            throw new EmailDeliveryException($"Could not write {file}.", ex);
        }

        LogQueued(message.Subject, file);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Queued e-mail '{Subject}' as {File}")]
    private partial void LogQueued(string subject, string file);
}
