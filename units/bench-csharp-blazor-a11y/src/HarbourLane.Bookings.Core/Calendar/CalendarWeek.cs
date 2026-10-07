namespace HarbourLane.Bookings.Calendar;

public sealed record CalendarWeek(DateOnly Monday, IReadOnlyList<DateOnly> Days, IReadOnlyList<int> Hours, IReadOnlyList<CalendarSlot> Slots)
{
    public CalendarSlot SlotAt(DateOnly day, int hour) =>
        Slots.First(s => s.Day == day && s.Hour == hour);

    public static DateOnly MondayOf(DateOnly day) =>
        day.AddDays(-(((int)day.DayOfWeek + 6) % 7));
}
