using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Quellbrook.Notifier.Channels;

/// <summary>Sends e-mail through the provider's v3 mail/send endpoint.</summary>
public sealed partial class EmailSender(HttpClient http, IOptions<EmailProviderOptions> options, ILogger<EmailSender> logger) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        var settings = options.Value;
        using var request = new HttpRequestMessage(HttpMethod.Post, "v3/mail/send")
        {
            Content = JsonContent.Create(new
            {
                personalizations = new[] { new { to = new[] { new { email = message.To, name = message.ToName } } } },
                from = new { email = settings.FromAddress, name = settings.FromName },
                subject = message.Subject,
                content = new[] { new { type = "text/plain", value = message.Body } },
                custom_args = new { order_id = message.OrderId.ToString() },
            }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        using var response = await http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new ProviderException($"The e-mail provider answered {(int)response.StatusCode} for order {message.OrderId}.");
        }

        var providerId = response.Headers.TryGetValues("X-Message-Id", out var ids) ? ids.FirstOrDefault() : null;
        LogAccepted(message.OrderId, message.To, providerId ?? "unknown");
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "E-mail for order {OrderId} accepted by the provider for {Recipient} ({ProviderMessageId})")]
    private partial void LogAccepted(Guid orderId, string recipient, string providerMessageId);
}
