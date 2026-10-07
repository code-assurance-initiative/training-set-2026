using HarbourLane.Bookings.Rooms;
using HarbourLane.Bookings.Web.Pages.Admin;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Abstractions;

namespace HarbourLane.Bookings.UnitTests.Admin;

public sealed class EditRoomModelTests
{
    private readonly InMemoryRoomCatalog _catalog = new(RoomSeed.Rooms);
    private readonly EditRoomModel _page;

    public EditRoomModelTests()
    {
        _page = new EditRoomModel(_catalog, NullLogger<EditRoomModel>.Instance)
        {
            PageContext = new PageContext { HttpContext = new DefaultHttpContext() },
        };
    }

    private static Room Hall => RoomSeed.Rooms[0];

    [Fact]
    public async Task Get_fills_the_form_from_the_room()
    {
        var result = await _page.OnGetAsync(Hall.Id, TestContext.Current.CancellationToken);

        Assert.IsType<PageResult>(result);
        Assert.Equal((Hall.Name, Hall.Capacity, Hall.HourlyRate), (_page.Input.Name, _page.Input.Capacity, _page.Input.HourlyRate));
    }

    [Fact]
    public async Task Get_for_an_unknown_room_is_not_found()
    {
        var result = await _page.OnGetAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Post_saves_the_room_and_returns_to_the_list()
    {
        _page.Input = new EditRoomModel.RoomInput { Name = " Great hall ", Capacity = 100, HourlyRate = 40m, DescriptionHtml = "<p>New floor.</p>" };

        var result = await _page.OnPostAsync(Hall.Id, TestContext.Current.CancellationToken);

        Assert.Equal("/Admin/Index", Assert.IsType<RedirectToPageResult>(result).PageName);
        var saved = await _catalog.FindAsync(Hall.Id, TestContext.Current.CancellationToken);
        Assert.Equal(("Great hall", 100), (saved?.Name, saved?.Capacity ?? 0));
    }

    [Fact]
    public async Task Post_with_invalid_input_shows_the_form_again_and_saves_nothing()
    {
        _page.ModelState.AddModelError("Input.Capacity", "The field Capacity must be between 1 and 500.");
        _page.Input = new EditRoomModel.RoomInput { Name = "Great hall", Capacity = 0, HourlyRate = 40m, DescriptionHtml = "<p>x</p>" };

        var result = await _page.OnPostAsync(Hall.Id, TestContext.Current.CancellationToken);

        Assert.IsType<PageResult>(result);
        Assert.Equal(Hall.Name, (await _catalog.FindAsync(Hall.Id, TestContext.Current.CancellationToken))?.Name);
    }

    [Fact]
    public async Task Post_for_an_unknown_room_is_not_found()
    {
        _page.Input = new EditRoomModel.RoomInput { Name = "Ghost room", Capacity = 1, HourlyRate = 1m, DescriptionHtml = "<p>x</p>" };

        var result = await _page.OnPostAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.IsType<NotFoundResult>(result);
    }
}
