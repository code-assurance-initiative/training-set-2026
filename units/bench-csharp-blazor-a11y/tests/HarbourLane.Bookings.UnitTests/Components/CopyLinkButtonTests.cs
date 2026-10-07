using HarbourLane.Bookings.UnitTests.TestSupport;
using HarbourLane.Bookings.Web.Components.Shared;
using Microsoft.JSInterop;

namespace HarbourLane.Bookings.UnitTests.Components;

public sealed class CopyLinkButtonTests : PortalContext
{
    private const string Url = "https://bookings.harbourlane.org/rooms/workshop";

    [Fact]
    public void The_link_is_copied_with_the_clipboard_api()
    {
        JSInterop.SetupVoid("navigator.clipboard.writeText", Url).SetVoidResult();
        var cut = Render<CopyLinkButton>(p => p.Add(c => c.Url, Url).Add(c => c.Label, "Copy a link to this room"));

        cut.Find("button").Click();

        Assert.Equal("Link copied.", cut.Find("[role=status]").TextContent);
        Assert.Equal("Copy a link to this room", cut.Find("button").GetAttribute("aria-label"));
    }

    [Fact]
    public void A_refusal_tells_the_visitor_how_to_copy_by_hand()
    {
        JSInterop.SetupVoid("navigator.clipboard.writeText", Url).SetException(new JSException("NotAllowedError"));
        var cut = Render<CopyLinkButton>(p => p.Add(c => c.Url, Url));

        cut.Find("button").Click();

        Assert.StartsWith("Your browser did not allow copying.", cut.Find("[role=status]").TextContent, StringComparison.Ordinal);
    }
}
