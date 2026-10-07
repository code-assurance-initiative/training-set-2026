using System.Net.Http.Json;

namespace ReportDesk.Api.Webhooks;

/// <summary>
/// Tells a tenant's callback that a scheduled report is ready. Its client connects only to addresses that
/// <see cref="CallbackAddressPolicy"/> accepts (see the hosting registration).
/// </summary>
public sealed partial class WebhookDispatcher(HttpClient client, ILogger<WebhookDispatcher> logger)
{
    public async Task<bool> NotifyAsync(Uri callback, ReportReadyEvent payload, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(callback);
        if (callback.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException("Webhook callbacks must use https.", nameof(callback));
        }

        using var response = await client.PostAsJsonAsync(callback, payload, cancellationToken).ConfigureAwait(false);
        LogDelivered(payload.ReportId, (int)response.StatusCode);
        return response.IsSuccessStatusCode;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Webhook for report {ReportId} answered {StatusCode}")]
    private partial void LogDelivered(Guid reportId, int statusCode);
}

public sealed record ReportReadyEvent(Guid ReportId, string Name, DateTimeOffset ReadyAt);
