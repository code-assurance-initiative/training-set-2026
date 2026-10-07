using HarbourLane.Bookings.Bookings;
using HarbourLane.Bookings.UnitTests.TestSupport;
using HarbourLane.Bookings.Web.Components.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace HarbourLane.Bookings.UnitTests.Components;

public sealed class MyBookingsPageTests : PortalContext
{
    private async Task<Booking> BookAsync(DateOnly date)
    {
        var outcome = await Services.GetRequiredService<BookingService>()
            .PlaceAsync(Requests.Valid(date), TestContext.Current.CancellationToken);
        return outcome.Booking ?? throw new InvalidOperationException("The test booking was rejected.");
    }

    [Fact]
    public void A_member_without_bookings_is_pointed_to_the_rooms()
    {
        var cut = Render<MyBookings>();

        Assert.Equal("Find a room to book", cut.Find("a[href=rooms]").TextContent);
    }

    [Fact]
    public async Task The_members_bookings_are_listed_with_their_reference()
    {
        var booking = await BookAsync(new DateOnly(2026, 10, 6));

        var cut = Render<MyBookings>();

        Assert.Equal("Main hall · Tuesday 6 October, 10:00", cut.Find(".booking-item h2").TextContent);
        Assert.Contains(booking.Reference, cut.Find(".booking-item p").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Cancelling_asks_first_and_then_releases_the_booking()
    {
        var booking = await BookAsync(new DateOnly(2026, 10, 6));
        var cut = Render<MyBookings>();

        cut.Find(".cancel-link").Click();
        Assert.Contains("will be released", cut.Find("[role=dialog] p").TextContent, StringComparison.Ordinal);
        cut.FindAll(".dialog-actions button")[0].Click();

        cut.WaitForAssertion(() => Assert.Contains("cancelled", cut.Find(".booking-item p").TextContent, StringComparison.Ordinal));
        Assert.Empty(cut.FindAll("[role=dialog]"));
        Assert.Equal(BookingStatus.Cancelled, (await Services.GetRequiredService<IBookingStore>().FindAsync(booking.Reference, TestContext.Current.CancellationToken))?.Status);
    }

    [Fact]
    public async Task The_filter_matches_room_names()
    {
        await BookAsync(new DateOnly(2026, 10, 6));
        var cut = Render<MyBookings>();

        cut.Find("input[type=search]").Input("workshop");

        Assert.Empty(cut.FindAll(".booking-item"));
    }
}
