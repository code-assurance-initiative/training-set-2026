using Depot.Slots.Core.Bookings;

namespace Depot.Slots.UnitTests.Bookings;

public sealed class InMemoryBookingStoreTests
{
    private static readonly DateTimeOffset Nine = new(2026, 3, 2, 9, 0, 0, TimeSpan.Zero);

    private static Booking Slot(DateTimeOffset start, int minutes, string dock = "D01") =>
        new(Guid.NewGuid(), dock, "CARR-1", start, start.AddMinutes(minutes), null);

    [Fact]
    public async Task BackToBackSlotsDoNotOverlap()
    {
        var store = new InMemoryBookingStore();
        var ct = TestContext.Current.CancellationToken;

        Assert.True(await store.TryAddAsync(Slot(Nine, 30), ct));
        Assert.True(await store.TryAddAsync(Slot(Nine.AddMinutes(30), 30), ct));
        Assert.False(await store.TryAddAsync(Slot(Nine.AddMinutes(15), 30), ct));
    }

    [Fact]
    public async Task DueRemindersExcludeAnnouncedAndLaterBookings()
    {
        var store = new InMemoryBookingStore();
        var ct = TestContext.Current.CancellationToken;
        var soon = Slot(Nine, 30);
        var announced = Slot(Nine, 30, "D02");
        var later = Slot(Nine.AddHours(3), 30);
        foreach (var booking in new[] { soon, announced, later })
        {
            await store.TryAddAsync(booking, ct);
        }

        await store.MarkRemindedAsync(announced.Id, Nine.AddMinutes(-30), ct);
        var due = await store.ListDueForReminderAsync(Nine.AddMinutes(30), ct);

        Assert.Equal([soon.Id], due.Select(b => b.Id));
    }
}
