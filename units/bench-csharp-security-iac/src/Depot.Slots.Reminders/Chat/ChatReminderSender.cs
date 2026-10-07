using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Depot.Slots.Core.Bookings;
using Depot.Slots.Core.Reminders;
using Microsoft.Extensions.Options;

namespace Depot.Slots.Reminders.Chat;

/// <summary>Posts "carrier X arrives at dock Y" to the yard team's channel.</summary>
public sealed class ChatReminderSender(HttpClient http, IOptions<ChatOptions> options) : IReminderSender
{
    public async Task SendAsync(Booking booking, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(booking);
        var chat = options.Value;
        var text = string.Create(
            CultureInfo.InvariantCulture,
            $"Dock {booking.DockCode}: carrier {booking.CarrierReference} is booked from {booking.StartsAt:HH:mm} to {booking.EndsAt:HH:mm} UTC.");

        using var request = new HttpRequestMessage(HttpMethod.Post, chat.Endpoint)
        {
            Content = JsonContent.Create(new ChatMessage(chat.Channel, text)),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", chat.BotToken);

        using var response = await http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var reply = await response.Content.ReadFromJsonAsync<ChatReply>(cancellationToken).ConfigureAwait(false);
        if (reply is not { Ok: true })
        {
            throw new HttpRequestException($"The chat service refused the message: {reply?.Error ?? "no reply"}.");
        }
    }

    private sealed record ChatMessage(
        [property: JsonPropertyName("channel")] string Channel,
        [property: JsonPropertyName("text")] string Text);

    private sealed record ChatReply(
        [property: JsonPropertyName("ok")] bool Ok,
        [property: JsonPropertyName("error")] string? Error);
}
