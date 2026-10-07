using HarbourLane.Bookings.Bookings;
using HarbourLane.Bookings.Rooms;
using HarbourLane.Bookings.UnitTests.TestSupport;

namespace HarbourLane.Bookings.UnitTests.Bookings;

public sealed class BookingServiceTests
{
    private readonly InMemoryBookingStore _store = new();
    private readonly BookingService _service;

    public BookingServiceTests()
    {
        _service = new BookingService(new InMemoryRoomCatalog(RoomSeed.Rooms), _store, FixedClock.AtMondayMorning());
    }

    private static DateOnly Tomorrow => new(2026, 10, 6);

    [Fact]
    public async Task A_valid_request_is_booked_with_a_readable_reference()
    {
        var outcome = await _service.PlaceAsync(Requests.Valid(Tomorrow), TestContext.Current.CancellationToken);

        Assert.True(outcome.Succeeded);
        Assert.Matches("^HL-[2-9A-HJ-NP-Z]{6}$", outcome.Booking?.Reference);
        Assert.Equal(new DateTimeOffset(2026, 10, 6, 10, 0, 0, TimeSpan.Zero), outcome.Booking?.Start);
        Assert.Equal(new DateTimeOffset(2026, 10, 6, 12, 0, 0, TimeSpan.Zero), outcome.Booking?.End);
    }

    [Fact]
    public async Task An_overlapping_request_for_the_same_room_is_a_conflict()
    {
        var ct = TestContext.Current.CancellationToken;
        await _service.PlaceAsync(Requests.Valid(Tomorrow), ct);
        var second = Requests.Valid(Tomorrow);
        second.StartTime = new TimeOnly(11, 0);

        var outcome = await _service.PlaceAsync(second, ct);

        Assert.False(outcome.Succeeded);
        Assert.True(outcome.Conflict);
    }

    [Fact]
    public async Task An_invalid_request_returns_its_errors_and_books_nothing()
    {
        var ct = TestContext.Current.CancellationToken;
        var request = Requests.Valid(Tomorrow);
        request.Attendees = 0;

        var outcome = await _service.PlaceAsync(request, ct);

        Assert.False(outcome.Succeeded);
        Assert.NotNull(outcome.Errors.For(BookingField.Attendees));
        Assert.Empty(await _service.ForOrganiserAsync(request.OrganiserEmail, ct));
    }

    [Fact]
    public async Task Only_the_organiser_can_cancel_and_the_slot_becomes_free()
    {
        var ct = TestContext.Current.CancellationToken;
        var booked = (await _service.PlaceAsync(Requests.Valid(Tomorrow), ct)).Booking;
        Assert.NotNull(booked);

        Assert.False(await _service.CancelAsync(booked.Reference, "someone.else@example.org", ct));
        Assert.True(await _service.CancelAsync(booked.Reference, booked.OrganiserEmail, ct));
        Assert.True((await _service.PlaceAsync(Requests.Valid(Tomorrow), ct)).Succeeded);
    }
}
