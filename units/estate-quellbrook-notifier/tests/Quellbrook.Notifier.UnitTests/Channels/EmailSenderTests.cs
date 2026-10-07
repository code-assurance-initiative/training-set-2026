using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Quellbrook.Notifier.Channels;
using Quellbrook.Notifier.UnitTests.TestSupport;

namespace Quellbrook.Notifier.UnitTests.Channels;

public sealed class EmailSenderTests
{
    private const string ProviderKey = "unit-test-provider-key";

    private static readonly EmailMessage Message =
        new(Guid.Parse("0198f1a2-0000-7000-8000-000000000042"), "maja.holm@post.example", "Maja Holm", "Subject", "Body");

    private static EmailSender Sender(StubHandler handler) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("https://email.test/") },
            Options.Create(new EmailProviderOptions { ApiKey = ProviderKey, FromAddress = "deliveries@quellbrook.example" }),
            NullLogger<EmailSender>.Instance);

    [Fact]
    public async Task TheMessageIsPostedToTheProviderWithTheKeyAsBearerToken()
    {
        var handler = new StubHandler(HttpStatusCode.Accepted, ("X-Message-Id", "abc123"));

        await Sender(handler).SendAsync(Message, TestContext.Current.CancellationToken);

        var (request, body) = Assert.Single(handler.Requests);
        Assert.Equal(new Uri("https://email.test/v3/mail/send"), request.RequestUri);
        Assert.Equal(("Bearer", ProviderKey), (request.Headers.Authorization?.Scheme, request.Headers.Authorization?.Parameter));
        Assert.Contains("\"email\":\"maja.holm@post.example\"", body, StringComparison.Ordinal);
        Assert.Contains("\"type\":\"text/plain\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ARefusalIsAProviderException()
    {
        var handler = new StubHandler(HttpStatusCode.Unauthorized);

        var exception = await Assert.ThrowsAsync<ProviderException>(() => Sender(handler).SendAsync(Message, TestContext.Current.CancellationToken));

        Assert.Contains("401", exception.Message, StringComparison.Ordinal);
    }
}
