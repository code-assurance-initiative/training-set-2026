using Depot.Slots.Core.Bookings;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace Depot.Slots.UnitTests.Bookings;

public sealed class BookingServiceTests
{
    private readonly FakeTimeProvider _clock = TestClock.Create();
    private readonly InMemoryBookingStore _store = new();
    private readonly BookingService _service;

    public BookingServiceTests()
    {
        var docks = Options.Create(new DockOptions { Codes = ["D01", "D02"], SlotMinutes = 15, MaxDurationMinutes = 240 });
        _service = new BookingService(_store, docks, _clock, NullLogger<BookingService>.Instance);
    }

    private DateTimeOffset At(int hour, int minute = 0) =>
        new DateTimeOffset(_clock.GetUtcNow().Date, TimeSpan.Zero).AddHours(hour).AddMinutes(minute);

    [Fact]
    public async Task BooksAFreeAlignedSlotAndNormalisesTheDockCode()
    {
        var outcome = await _service.BookAsync(new BookingRequest("d01", "CARR-1001", At(9), 45), TestContext.Current.CancellationToken);

        Assert.True(outcome.Succeeded);
        var booking = Assert.IsType<Booking>(outcome.Booking);
        Assert.Equal("D01", booking.DockCode);
        Assert.Equal(At(9, 45), booking.EndsAt);
        Assert.Null(booking.ReminderSentAt);
    }

    [Fact]
    public async Task RefusesAnUnknownDock()
    {
        var outcome = await _service.BookAsync(new BookingRequest("D09", "CARR-1001", At(9), 30), TestContext.Current.CancellationToken);

        Assert.Equal(BookingFailure.UnknownDock, outcome.Failure);
    }

    [Theory]
    [InlineData(10, 30)]
    [InlineData(0, 20)]
    [InlineData(0, 255)]
    public async Task RefusesAWindowOffTheSlotGrid(int startMinute, int duration)
    {
        var outcome = await _service.BookAsync(
            new BookingRequest("D01", "CARR-1001", At(9, startMinute), duration), TestContext.Current.CancellationToken);

        Assert.Equal(BookingFailure.InvalidWindow, outcome.Failure);
    }

    [Fact]
    public async Task RefusesASlotThatHasAlreadyStarted()
    {
        var outcome = await _service.BookAsync(new BookingRequest("D01", "CARR-1001", At(6), 30), TestContext.Current.CancellationToken);

        Assert.Equal(BookingFailure.InThePast, outcome.Failure);
    }

    [Fact]
    public async Task RefusesAnOverlappingSlotOnTheSameDockButNotOnAnother()
    {
        var ct = TestContext.Current.CancellationToken;
        await _service.BookAsync(new BookingRequest("D01", "CARR-1001", At(9), 60), ct);

        var clash = await _service.BookAsync(new BookingRequest("D01", "CARR-2002", At(9, 30), 60), ct);
        var otherDock = await _service.BookAsync(new BookingRequest("D02", "CARR-2002", At(9, 30), 60), ct);

        Assert.Equal(BookingFailure.Conflict, clash.Failure);
        Assert.True(otherDock.Succeeded);
    }

    [Fact]
    public async Task ListsOneDayOfOneDockInStartOrder()
    {
        var ct = TestContext.Current.CancellationToken;
        await _service.BookAsync(new BookingRequest("D01", "CARR-B", At(14), 30), ct);
        await _service.BookAsync(new BookingRequest("D01", "CARR-A", At(8), 30), ct);
        await _service.BookAsync(new BookingRequest("D02", "CARR-C", At(8), 30), ct);

        var day = await _service.ListDayAsync("d01", DateOnly.FromDateTime(At(0).UtcDateTime), ct);

        Assert.Equal(["CARR-A", "CARR-B"], day.Select(b => b.CarrierReference));
    }

    [Fact]
    public async Task CancellingFreesTheWindow()
    {
        var ct = TestContext.Current.CancellationToken;
        var first = Assert.IsType<Booking>((await _service.BookAsync(new BookingRequest("D01", "CARR-1001", At(9), 60), ct)).Booking);

        Assert.True(await _service.CancelAsync(first.Id, ct));
        Assert.False(await _service.CancelAsync(first.Id, ct));
        Assert.True((await _service.BookAsync(new BookingRequest("D01", "CARR-2002", At(9), 60), ct)).Succeeded);
    }
}
