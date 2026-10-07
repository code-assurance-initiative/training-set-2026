using HarbourLane.Bookings.Bookings;

namespace HarbourLane.Bookings.Calendar;

public sealed class WeekCalendarBuilder(IBookingStore store, TimeProvider clock)
{
    public async Task<CalendarWeek> BuildAsync(Guid roomId, DateOnly anyDayOfWeek, CancellationToken cancellationToken)
    {
        var monday = CalendarWeek.MondayOf(anyDayOfWeek);
        var days = Enumerable.Range(0, 7).Select(monday.AddDays).ToList();
        var hours = OpeningHours.Hours.ToList();

        var from = At(monday, 0);
        var bookings = await store.ListAsync(from, from.AddDays(7), cancellationToken).ConfigureAwait(false);
        var roomBookings = bookings.Where(b => b.RoomId == roomId).ToList();

        var slots = new List<CalendarSlot>(days.Count * hours.Count);
        foreach (var day in days)
        {
            foreach (var hour in hours)
            {
                var start = At(day, hour);
                var holder = roomBookings.FirstOrDefault(b => b.Overlaps(start, start.AddHours(1)));
                slots.Add(new CalendarSlot(day, hour, holder));
            }
        }

        return new CalendarWeek(monday, days, hours, slots);
    }

    private DateTimeOffset At(DateOnly day, int hour)
    {
        var local = day.ToDateTime(new TimeOnly(hour, 0));
        return new DateTimeOffset(local, clock.LocalTimeZone.GetUtcOffset(local));
    }
}
