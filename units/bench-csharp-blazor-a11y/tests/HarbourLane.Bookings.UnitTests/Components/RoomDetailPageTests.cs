using HarbourLane.Bookings.UnitTests.TestSupport;
using HarbourLane.Bookings.Web.Components.Pages;

namespace HarbourLane.Bookings.UnitTests.Components;

public sealed class RoomDetailPageTests : PortalContext
{
    public RoomDetailPageTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void The_room_page_shows_the_room_and_draws_its_map()
    {
        var cut = Render<RoomDetail>(p => p.Add(c => c.Slug, "main-hall"));

        Assert.Equal("Main hall", cut.Find("h1").TextContent);
        Assert.Equal("book/main-hall", cut.Find(".room-actions a").GetAttribute("href"));
        Assert.Equal("Sprung wooden floor, stage with steps and a ramp, stackable chairs for 120.", cut.Find(".room-description p").TextContent);
        Assert.Single(JSInterop.Invocations, i => i.Identifier == "roomMap.render");
    }

    [Fact]
    public void Choosing_a_thumbnail_shows_that_photo()
    {
        var cut = Render<RoomDetail>(p => p.Add(c => c.Slug, "main-hall"));

        cut.FindAll(".gallery-thumb")[1].Click();

        Assert.Equal("https://media.harbourlane.org/rooms/main-hall-2.jpg", cut.Find(".gallery-photo").GetAttribute("src"));
        Assert.Equal("true", cut.FindAll(".gallery-thumb")[1].GetAttribute("aria-pressed"));
    }

    [Fact]
    public void An_unknown_room_says_so()
    {
        var cut = Render<RoomDetail>(p => p.Add(c => c.Slug, "ballroom"));

        Assert.Equal("Room not found", cut.Find("h1").TextContent);
    }
}
