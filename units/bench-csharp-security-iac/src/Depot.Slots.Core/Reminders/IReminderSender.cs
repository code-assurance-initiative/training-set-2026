using Depot.Slots.Core.Bookings;

namespace Depot.Slots.Core.Reminders;

/// <summary>Tells the yard team that a carrier is about to arrive.</summary>
public interface IReminderSender
{
    Task SendAsync(Booking booking, CancellationToken cancellationToken);
}
