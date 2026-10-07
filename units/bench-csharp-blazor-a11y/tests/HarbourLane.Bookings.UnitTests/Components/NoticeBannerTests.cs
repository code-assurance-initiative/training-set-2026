using HarbourLane.Bookings.Content;
using HarbourLane.Bookings.UnitTests.TestSupport;
using HarbourLane.Bookings.Web.Components.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace HarbourLane.Bookings.UnitTests.Components;

public sealed class NoticeBannerTests : PortalContext
{
    [Fact]
    public void An_empty_notice_renders_nothing()
    {
        var cut = Render<NoticeBanner>();

        Assert.Empty(cut.FindAll("aside"));
    }

    [Fact]
    public async Task The_notice_is_rendered_without_anything_executable()
    {
        await Services.GetRequiredService<ISiteNoticeStore>()
            .SetAsync("<p><strong>Closed</strong> on Monday.<script>alert(1)</script></p>", TestContext.Current.CancellationToken);

        var cut = Render<NoticeBanner>();

        cut.Find("aside").MarkupMatches("<aside class=\"notice\" aria-label=\"Notice\"><p><strong>Closed</strong> on Monday.</p></aside>");
    }
}
