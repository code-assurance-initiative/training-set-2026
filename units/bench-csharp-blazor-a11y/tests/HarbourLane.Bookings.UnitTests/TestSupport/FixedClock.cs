namespace HarbourLane.Bookings.UnitTests.TestSupport;

/// <summary>A clock stopped at a known local time in UTC, so dates in tests do not depend on the machine.</summary>
internal sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public static FixedClock AtMondayMorning() => new(new DateTimeOffset(2026, 10, 5, 9, 0, 0, TimeSpan.Zero));

    public override DateTimeOffset GetUtcNow() => now;

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}
