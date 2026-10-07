using HarbourLane.Bookings.Rooms;

namespace HarbourLane.Bookings.UnitTests.Rooms;

public sealed class RoomFilterTests
{
    [Fact]
    public void No_filter_returns_every_room_by_name()
    {
        var rooms = RoomFilter.None.Apply(RoomSeed.Rooms);

        Assert.Equal(["Harbour room", "Main hall", "Workshop"], rooms.Select(r => r.Name));
    }

    [Fact]
    public void Text_matches_name_or_floor_ignoring_case()
    {
        var byFloor = new RoomFilter("first FLOOR", new HashSet<Amenity>(), 0).Apply(RoomSeed.Rooms);

        Assert.Equal("Harbour room", Assert.Single(byFloor).Name);
    }

    [Fact]
    public void Every_selected_amenity_must_be_present()
    {
        var filter = new RoomFilter(null, new HashSet<Amenity> { Amenity.Kitchen, Amenity.Projector }, 0);

        Assert.Equal("Main hall", Assert.Single(filter.Apply(RoomSeed.Rooms)).Name);
    }

    [Fact]
    public void Minimum_capacity_excludes_smaller_rooms()
    {
        var rooms = new RoomFilter(null, new HashSet<Amenity>(), 20).Apply(RoomSeed.Rooms);

        Assert.Equal(["Main hall", "Workshop"], rooms.Select(r => r.Name));
    }
}
