using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ParcelTracking.Core.Notifications;

namespace ParcelTracking.Infrastructure.Notifications;

/// <summary>Posts parcel status changes to the merchant-hooks relay, signed with HMAC-SHA256.</summary>
public sealed class MerchantWebhookClient(HttpClient httpClient, IOptions<WebhookOptions> options) : IMerchantNotifier
{
    public const string SignatureHeader = "X-Signature-SHA256";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task NotifyAsync(PendingNotification notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var payload = new StatusChangedPayload(
            notification.Id,
            notification.TrackingNumber,
            notification.Status.ToString(),
            notification.OccurredAt);
        var body = JsonSerializer.SerializeToUtf8Bytes(payload, SerializerOptions);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"merchants/{Uri.EscapeDataString(notification.MerchantId)}/events")
        {
            Content = new ByteArrayContent(body),
        };
        request.Content.Headers.ContentType = new("application/json");
        request.Headers.Add(SignatureHeader, Sign(body, options.Value.SigningKey));

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public static string Sign(byte[] body, string signingKey) =>
        Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(signingKey), body));

    private sealed record StatusChangedPayload(Guid EventId, string TrackingNumber, string Status, DateTimeOffset OccurredAt);
}
