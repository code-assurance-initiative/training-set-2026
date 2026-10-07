using HarbourLane.Bookings.Bookings;
using HarbourLane.Bookings.Calendar;
using HarbourLane.Bookings.Rooms;
using HarbourLane.Bookings.UnitTests.TestSupport;

namespace HarbourLane.Bookings.UnitTests.Calendar;

public sealed class WeekCalendarBuilderTests
{
    [Theory]
    [InlineData("2026-10-05", "2026-10-05")]
    [InlineData("2026-10-08", "2026-10-05")]
    [InlineData("2026-10-11", "2026-10-05")]
    public void The_week_starts_on_monday(string day, string monday)
    {
        Assert.Equal(DateOnly.Parse(monday, System.Globalization.CultureInfo.InvariantCulture), CalendarWeek.MondayOf(DateOnly.Parse(day, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Fact]
    public async Task Slots_held_by_a_booking_of_the_room_are_marked_with_it()
    {
        var ct = TestContext.Current.CancellationToken;
        var store = new InMemoryBookingStore();
        var clock = FixedClock.AtMondayMorning();
        var booking = (await new BookingService(new InMemoryRoomCatalog(RoomSeed.Rooms), store, clock)
            .PlaceAsync(Requests.Valid(new DateOnly(2026, 10, 7)), ct)).Booking;

        var week = await new WeekCalendarBuilder(store, clock).BuildAsync(Requests.Hall.Id, new DateOnly(2026, 10, 7), ct);

        Assert.Equal(7 * 14, week.Slots.Count);
        Assert.Equal(booking, week.SlotAt(new DateOnly(2026, 10, 7), 10).Booking);
        Assert.Equal(booking, week.SlotAt(new DateOnly(2026, 10, 7), 11).Booking);
        Assert.True(week.SlotAt(new DateOnly(2026, 10, 7), 12).IsFree);
    }

    [Fact]
    public async Task Bookings_of_other_rooms_leave_the_slot_free()
    {
        var ct = TestContext.Current.CancellationToken;
        var store = new InMemoryBookingStore();
        var clock = FixedClock.AtMondayMorning();
        await new BookingService(new InMemoryRoomCatalog(RoomSeed.Rooms), store, clock).PlaceAsync(Requests.Valid(new DateOnly(2026, 10, 7)), ct);

        var week = await new WeekCalendarBuilder(store, clock).BuildAsync(RoomSeed.Rooms[1].Id, new DateOnly(2026, 10, 7), ct);

        Assert.All(week.Slots, slot => Assert.True(slot.IsFree));
    }
}
