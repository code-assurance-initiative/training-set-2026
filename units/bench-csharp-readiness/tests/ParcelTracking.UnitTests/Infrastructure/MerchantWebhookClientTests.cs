using System.Net;
using Microsoft.Extensions.Options;
using ParcelTracking.Core.Notifications;
using ParcelTracking.Core.Parcels;
using ParcelTracking.Infrastructure.Notifications;
using ParcelTracking.UnitTests.TestSupport;

namespace ParcelTracking.UnitTests.Infrastructure;

public sealed class MerchantWebhookClientTests
{
    private const string SigningKey = "unit-tests-signing-key-not-used-anywhere-else";

    [Fact]
    public async Task Posts_a_signed_event_to_the_merchant_path()
    {
        var handler = StubHttpHandler.Returning(HttpStatusCode.Accepted);
        var client = new MerchantWebhookClient(
            new HttpClient(handler) { BaseAddress = new Uri("https://hooks.test/") },
            Options.Create(new WebhookOptions { BaseAddress = new Uri("https://hooks.test/"), SigningKey = SigningKey }));
        var notification = new PendingNotification(Guid.NewGuid(), Guid.NewGuid(), "merchant 7", "NP100200300", ParcelStatus.Delivered, DateTimeOffset.UnixEpoch);

        await client.NotifyAsync(notification, TestContext.Current.CancellationToken);

        var (request, body) = Assert.Single(handler.Requests);
        Assert.NotNull(body);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/merchants/merchant%207/events", request.RequestUri?.AbsolutePath);
        Assert.Contains("\"status\":\"Delivered\"", body, StringComparison.Ordinal);
        var signature = Assert.Single(request.Headers.GetValues(MerchantWebhookClient.SignatureHeader));
        Assert.Equal(MerchantWebhookClient.Sign(System.Text.Encoding.UTF8.GetBytes(body), SigningKey), signature);
    }

    [Fact]
    public async Task A_rejected_delivery_throws()
    {
        var client = new MerchantWebhookClient(
            new HttpClient(StubHttpHandler.Returning(HttpStatusCode.InternalServerError)) { BaseAddress = new Uri("https://hooks.test/") },
            Options.Create(new WebhookOptions { BaseAddress = new Uri("https://hooks.test/"), SigningKey = SigningKey }));
        var notification = new PendingNotification(Guid.NewGuid(), Guid.NewGuid(), "m", "NP1", ParcelStatus.InTransit, DateTimeOffset.UnixEpoch);

        await Assert.ThrowsAsync<HttpRequestException>(() => client.NotifyAsync(notification, TestContext.Current.CancellationToken));
    }
}
