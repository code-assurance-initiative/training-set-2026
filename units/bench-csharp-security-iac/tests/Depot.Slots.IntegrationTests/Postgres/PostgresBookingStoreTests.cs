using Depot.Slots.Core.Bookings;
using Depot.Slots.Infrastructure.Postgres;

namespace Depot.Slots.IntegrationTests.Postgres;

public sealed class PostgresBookingStoreTests(PostgresFixture database) : IClassFixture<PostgresFixture>
{
    private static readonly DateTimeOffset Nine = new(2026, 3, 2, 9, 0, 0, TimeSpan.Zero);

    private PostgresBookingStore Store()
    {
        Assert.SkipWhen(database.DataSource is null, database.UnavailableReason ?? "PostgreSQL unavailable");
        return new PostgresBookingStore(database.DataSource ?? throw new InvalidOperationException());
    }

    private static Booking Slot(string dock, DateTimeOffset start, int minutes) =>
        new(Guid.NewGuid(), dock, "CARR-PG", start, start.AddMinutes(minutes), null);

    [Fact]
    public async Task TheExclusionConstraintRefusesAnOverlapOnTheSameDockOnly()
    {
        var store = Store();
        var ct = TestContext.Current.CancellationToken;

        Assert.True(await store.TryAddAsync(Slot("P01", Nine, 60), ct));
        Assert.False(await store.TryAddAsync(Slot("P01", Nine.AddMinutes(30), 60), ct));
        Assert.True(await store.TryAddAsync(Slot("P01", Nine.AddMinutes(60), 30), ct));
        Assert.True(await store.TryAddAsync(Slot("P02", Nine.AddMinutes(30), 60), ct));

        var day = await store.ListForDockAsync("P01", Nine.AddHours(-9), Nine.AddHours(15), ct);
        Assert.Equal([Nine, Nine.AddMinutes(60)], day.Select(b => b.StartsAt));
    }

    [Fact]
    public async Task RemindersAreListedUntilMarkedAndBookingsCanBeRemoved()
    {
        var store = Store();
        var ct = TestContext.Current.CancellationToken;
        var booking = Slot("P03", Nine, 30);
        await store.TryAddAsync(booking, ct);

        Assert.Contains(booking.Id, (await store.ListDueForReminderAsync(Nine, ct)).Select(b => b.Id));
        await store.MarkRemindedAsync(booking.Id, Nine.AddMinutes(-30), ct);
        Assert.DoesNotContain(booking.Id, (await store.ListDueForReminderAsync(Nine, ct)).Select(b => b.Id));

        Assert.True(await store.RemoveAsync(booking.Id, ct));
        Assert.False(await store.RemoveAsync(booking.Id, ct));
    }
}
