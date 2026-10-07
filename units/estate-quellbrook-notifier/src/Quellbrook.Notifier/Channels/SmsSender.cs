using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quellbrook.Notifier.Notifications;

namespace Quellbrook.Notifier.Channels;

/// <summary>Sends SMS through the gateway's v1 messages endpoint.</summary>
public sealed partial class SmsSender(HttpClient http, IOptions<SmsProviderOptions> options, ILogger<SmsSender> logger) : ISmsSender
{
    public async Task SendAsync(SmsMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        var settings = options.Value;
        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/messages")
        {
            Content = JsonContent.Create(new { to = message.To, from = settings.Sender, text = message.Text, reference = message.OrderId }),
        };
        request.Headers.Add("X-Api-Key", settings.ApiKey);
        using var response = await http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new ProviderException($"The SMS gateway answered {(int)response.StatusCode} for order {message.OrderId}.");
        }

        var maskedRecipient = ContactMask.Phone(message.To);
        LogAccepted(message.OrderId, maskedRecipient);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "SMS for order {OrderId} accepted by the gateway for {MaskedRecipient}")]
    private partial void LogAccepted(Guid orderId, string maskedRecipient);
}
