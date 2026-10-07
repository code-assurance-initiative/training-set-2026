using HarbourLane.Bookings.Content;
using HarbourLane.Bookings.Rooms;
using HarbourLane.Bookings.UnitTests.TestSupport;
using HarbourLane.Bookings.Web.Pages.Admin;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Abstractions;

namespace HarbourLane.Bookings.UnitTests.Admin;

public sealed class AdminIndexAndNoticeTests
{
    [Fact]
    public async Task The_staff_index_lists_every_room()
    {
        var page = new IndexModel(new InMemoryRoomCatalog(RoomSeed.Rooms));

        await page.OnGetAsync(TestContext.Current.CancellationToken);

        Assert.Equal(RoomSeed.Rooms.Count, page.Rooms.Count);
    }

    [Fact]
    public async Task The_notice_page_shows_and_saves_the_notice()
    {
        var ct = TestContext.Current.CancellationToken;
        var store = new InMemorySiteNoticeStore(FixedClock.AtMondayMorning());
        await store.SetAsync("<p>Closed on Monday.</p>", ct);
        var page = new NoticeModel(store, NullLogger<NoticeModel>.Instance)
        {
            PageContext = new PageContext { HttpContext = new DefaultHttpContext() },
        };

        await page.OnGetAsync(ct);
        Assert.Equal("<p>Closed on Monday.</p>", page.Html);

        page.Html = "<p>Open as usual.</p>";
        var result = await page.OnPostAsync(ct);

        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("<p>Open as usual.</p>", (await store.GetAsync(ct)).Html);
    }
}
