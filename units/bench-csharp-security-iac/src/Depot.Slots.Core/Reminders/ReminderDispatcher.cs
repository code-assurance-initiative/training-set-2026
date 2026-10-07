using Depot.Slots.Core.Bookings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Depot.Slots.Core.Reminders;

/// <summary>
/// One reminder pass: every booking that starts within the lead time and has not been announced is sent once and
/// then marked. A failed send leaves the booking unmarked, so the next pass retries it.
/// </summary>
public sealed partial class ReminderDispatcher(
    IBookingStore store,
    IReminderSender sender,
    IOptions<ReminderOptions> options,
    TimeProvider clock,
    ILogger<ReminderDispatcher> logger)
{
    public async Task<int> DispatchDueAsync(CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var due = await store.ListDueForReminderAsync(now + options.Value.LeadTime, cancellationToken).ConfigureAwait(false);
        var sent = 0;
        foreach (var booking in due)
        {
            if (booking.EndsAt <= now)
            {
                continue;
            }

            try
            {
                await sender.SendAsync(booking, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                LogSendFailed(ex, booking.Id);
                continue;
            }

            await store.MarkRemindedAsync(booking.Id, clock.GetUtcNow(), cancellationToken).ConfigureAwait(false);
            sent++;
        }

        return sent;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Reminder for booking {BookingId} failed; retrying next pass")]
    private partial void LogSendFailed(Exception exception, Guid bookingId);
}
