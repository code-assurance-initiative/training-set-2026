using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Quellbrook.Notifier.Channels;
using Quellbrook.Notifier.UnitTests.TestSupport;

namespace Quellbrook.Notifier.UnitTests.Channels;

public sealed class SmsSenderTests
{
    private static SmsSender Sender(StubHandler handler) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("https://sms.test/") },
            Options.Create(new SmsProviderOptions { ApiKey = "unit-test-gateway-key" }),
            NullLogger<SmsSender>.Instance);

    [Fact]
    public async Task TheTextIsPostedWithTheKeyAndTheSenderName()
    {
        var handler = new StubHandler(HttpStatusCode.Accepted);

        await Sender(handler).SendAsync(new SmsMessage(Guid.NewGuid(), "+4520304050", "Out for delivery"), TestContext.Current.CancellationToken);

        var (request, body) = Assert.Single(handler.Requests);
        Assert.Equal(new Uri("https://sms.test/v1/messages"), request.RequestUri);
        Assert.Equal("unit-test-gateway-key", request.Headers.GetValues("X-Api-Key").Single());
        Assert.Contains("\"from\":\"Quellbrook\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ARefusalIsAProviderException()
    {
        var handler = new StubHandler(HttpStatusCode.BadRequest);

        await Assert.ThrowsAsync<ProviderException>(() =>
            Sender(handler).SendAsync(new SmsMessage(Guid.NewGuid(), "+4520304050", "text"), TestContext.Current.CancellationToken));
    }
}
