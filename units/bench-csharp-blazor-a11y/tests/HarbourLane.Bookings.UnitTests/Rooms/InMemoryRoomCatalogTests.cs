using HarbourLane.Bookings.Rooms;

namespace HarbourLane.Bookings.UnitTests.Rooms;

public sealed class InMemoryRoomCatalogTests
{
    private readonly InMemoryRoomCatalog _catalog = new(RoomSeed.Rooms);

    [Fact]
    public async Task Rooms_are_found_by_slug_ignoring_case()
    {
        var room = await _catalog.FindBySlugAsync("MAIN-HALL", TestContext.Current.CancellationToken);

        Assert.Equal("Main hall", room?.Name);
    }

    [Fact]
    public async Task An_update_replaces_the_editable_fields_only()
    {
        var ct = TestContext.Current.CancellationToken;
        var hall = RoomSeed.Rooms[0];

        var updated = await _catalog.UpdateAsync(new RoomUpdate(hall.Id, "Great hall", 110, 50m, "<p>Renovated.</p>"), ct);
        var room = await _catalog.FindAsync(hall.Id, ct);

        Assert.True(updated);
        Assert.Equal(("Great hall", 110, 50m, "<p>Renovated.</p>"), (room?.Name, room?.Capacity ?? 0, room?.HourlyRate ?? 0m, room?.DescriptionHtml));
        Assert.Equal(hall.Slug, room?.Slug);
    }

    [Fact]
    public async Task Updating_an_unknown_room_reports_false()
    {
        var updated = await _catalog.UpdateAsync(new RoomUpdate(Guid.NewGuid(), "x", 1, 1m, "x"), TestContext.Current.CancellationToken);

        Assert.False(updated);
    }
}
