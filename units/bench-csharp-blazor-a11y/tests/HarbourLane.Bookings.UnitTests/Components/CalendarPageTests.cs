using HarbourLane.Bookings.Bookings;
using HarbourLane.Bookings.UnitTests.TestSupport;
using CalendarPage = HarbourLane.Bookings.Web.Components.Pages.Calendar;
using Microsoft.Extensions.DependencyInjection;

namespace HarbourLane.Bookings.UnitTests.Components;

public sealed class CalendarPageTests : PortalContext
{
    [Fact]
    public void The_current_week_is_shown_from_monday()
    {
        var cut = Render<CalendarPage>();

        Assert.Equal("Week of 5 October 2026", cut.Find(".week-title").TextContent);
        Assert.Equal(8, cut.FindAll("thead th").Count);
    }

    [Fact]
    public async Task A_booked_slot_opens_the_booking_details()
    {
        var request = Requests.Valid(new DateOnly(2026, 10, 7));
        request.RoomId = Guid.Parse("9b6a1d3c-27e4-4b8f-8c1d-5e2a7f9c3d02");
        request.Attendees = 8;
        var booking = (await Services.GetRequiredService<BookingService>().PlaceAsync(request, TestContext.Current.CancellationToken)).Booking;
        var cut = Render<CalendarPage>();

        cut.FindAll(".slot-booked")[0].Click();

        Assert.Equal($"Booking {booking?.Reference}", cut.Find("#booking-panel-title").TextContent);
        Assert.Equal(2, cut.FindAll(".slot-booked").Count);
    }

    [Fact]
    public void The_week_buttons_move_by_seven_days()
    {
        var cut = Render<CalendarPage>();

        cut.Find("button[aria-label='Previous week']").Click();

        Assert.Equal("Week of 28 September 2026", cut.Find(".week-title").TextContent);
    }
}
