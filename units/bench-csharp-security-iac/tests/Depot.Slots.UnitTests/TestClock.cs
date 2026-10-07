using Microsoft.Extensions.Time.Testing;

namespace Depot.Slots.UnitTests;

internal static class TestClock
{
    /// <summary>Monday 2 March 2026, 06:00 UTC: the yard opens.</summary>
    public static FakeTimeProvider Create() => new(new DateTimeOffset(2026, 3, 2, 6, 0, 0, TimeSpan.Zero));
}
