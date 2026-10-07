using HarbourLane.Bookings.Rooms;
using HarbourLane.Bookings.Web.Components.Shared;
using Microsoft.AspNetCore.Components.Web;

namespace HarbourLane.Bookings.UnitTests.Components;

public sealed class AmenityChipsTests : BunitContext
{
    private IReadOnlySet<Amenity> _selected = new HashSet<Amenity>();

    [Fact]
    public void Clicking_a_chip_selects_its_amenity()
    {
        var cut = RenderChips();

        cut.FindAll("[role=checkbox]")[0].Click();

        Assert.Equal([Amenity.Projector], _selected);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("Enter")]
    public void Space_and_enter_toggle_the_focused_chip(string key)
    {
        _selected = new HashSet<Amenity> { Amenity.Kitchen };
        var cut = RenderChips();

        cut.FindAll("[role=checkbox]")[2].KeyDown(new KeyboardEventArgs { Key = key });

        Assert.Empty(_selected);
    }

    [Fact]
    public void Other_keys_change_nothing()
    {
        var cut = RenderChips();

        cut.FindAll("[role=checkbox]")[0].KeyDown(new KeyboardEventArgs { Key = "Tab" });

        Assert.Empty(_selected);
    }

    [Fact]
    public void Each_chip_exposes_its_state_and_is_focusable()
    {
        _selected = new HashSet<Amenity> { Amenity.HearingLoop };
        var chips = RenderChips().FindAll("[role=checkbox]");

        Assert.Equal(Enum.GetValues<Amenity>().Length, chips.Count);
        Assert.All(chips, chip => Assert.Equal("0", chip.GetAttribute("tabindex")));
        Assert.Equal("true", chips.Single(c => c.TextContent == "Hearing loop").GetAttribute("aria-checked"));
        Assert.Equal("false", chips.Single(c => c.TextContent == "Projector").GetAttribute("aria-checked"));
    }

    private IRenderedComponent<AmenityChips> RenderChips() =>
        Render<AmenityChips>(p => p
            .Add(c => c.Selected, _selected)
            .Add(c => c.SelectedChanged, selected => _selected = selected));
}
