using HarbourLane.Bookings.UnitTests.TestSupport;
using RoomsPage = HarbourLane.Bookings.Web.Components.Pages.Rooms;

namespace HarbourLane.Bookings.UnitTests.Components;

public sealed class RoomsPageTests : PortalContext
{
    [Fact]
    public void Every_room_is_listed_with_a_link_to_its_page()
    {
        var cut = Render<RoomsPage>();

        var links = cut.FindAll(".room-card h3 a").Select(a => (a.TextContent, a.GetAttribute("href")));
        Assert.Equal([("Harbour room", "rooms/harbour-room"), ("Main hall", "rooms/main-hall"), ("Workshop", "rooms/workshop")], links);
        Assert.Equal("3 rooms match", cut.Find("p[role=status]").TextContent);
    }

    [Fact]
    public void Searching_narrows_the_list()
    {
        var cut = Render<RoomsPage>();

        cut.Find("#room-search").Input("work");

        Assert.Equal("Workshop", cut.Find(".room-card h3").TextContent);
        Assert.Equal("1 room matches", cut.Find("p[role=status]").TextContent);
    }

    [Fact]
    public void Selecting_an_amenity_keeps_only_rooms_that_have_it()
    {
        var cut = Render<RoomsPage>();

        cut.FindAll("[role=checkbox]").Single(c => c.TextContent == "Video conferencing").Click();

        Assert.Equal("Harbour room", cut.Find(".room-card h3").TextContent);
    }

    [Fact]
    public void The_search_field_has_a_label()
    {
        var cut = Render<RoomsPage>();

        Assert.Equal("Search rooms by name or floor", cut.Find("label[for=room-search]").TextContent);
    }
}
