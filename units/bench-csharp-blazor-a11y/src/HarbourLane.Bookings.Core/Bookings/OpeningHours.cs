namespace HarbourLane.Bookings.Bookings;

public static class OpeningHours
{
    public static TimeOnly Opens { get; } = new(8, 0);

    public static TimeOnly Closes { get; } = new(22, 0);

    public const int MaximumHours = 6;

    public static IEnumerable<int> Hours => Enumerable.Range(Opens.Hour, Closes.Hour - Opens.Hour);
}
