using System.Net;
using System.Text;
using System.Text.Json;
using Depot.Slots.Core.Bookings;
using Depot.Slots.Reminders.Chat;
using Microsoft.Extensions.Options;

namespace Depot.Slots.UnitTests.Chat;

public sealed class ChatReminderSenderTests
{
    private static readonly Booking Arrival = new(
        Guid.NewGuid(), "D03", "CARR-7781",
        new DateTimeOffset(2026, 3, 2, 9, 15, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 3, 2, 10, 0, 0, TimeSpan.Zero),
        null);

    private static IOptions<ChatOptions> Settings(string token) => Options.Create(new ChatOptions
    {
        Endpoint = "https://chat.test/api/chat.postMessage",
        Channel = "#yard-arrivals",
        BotToken = token,
    });

    [Fact]
    public async Task PostsTheArrivalToTheChannelWithTheConfiguredBearerToken()
    {
        // A token made up for this test; the stub handler below is the only thing that ever sees it.
        var token = Guid.NewGuid().ToString("N");
        var handler = new StubHandler("""{"ok":true}""");
        var sender = new ChatReminderSender(new HttpClient(handler), Settings(token));

        await sender.SendAsync(Arrival, TestContext.Current.CancellationToken);

        Assert.Equal("Bearer", handler.Authorization?.Scheme);
        Assert.Equal(token, handler.Authorization?.Parameter);
        using var body = JsonDocument.Parse(handler.Body ?? "{}");
        Assert.Equal("#yard-arrivals", body.RootElement.GetProperty("channel").GetString());
        Assert.Equal(
            "Dock D03: carrier CARR-7781 is booked from 09:15 to 10:00 UTC.",
            body.RootElement.GetProperty("text").GetString());
    }

    [Fact]
    public async Task ARefusedMessageSurfacesAsARequestFailure()
    {
        var handler = new StubHandler("""{"ok":false,"error":"channel_not_found"}""");
        var sender = new ChatReminderSender(new HttpClient(handler), Settings("t"));

        var error = await Assert.ThrowsAsync<HttpRequestException>(
            () => sender.SendAsync(Arrival, TestContext.Current.CancellationToken));
        Assert.Contains("channel_not_found", error.Message, StringComparison.Ordinal);
    }

    private sealed class StubHandler(string reply) : HttpMessageHandler
    {
        public System.Net.Http.Headers.AuthenticationHeaderValue? Authorization { get; private set; }

        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Authorization = request.Headers.Authorization;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(reply, Encoding.UTF8, "application/json"),
            };
        }
    }
}
