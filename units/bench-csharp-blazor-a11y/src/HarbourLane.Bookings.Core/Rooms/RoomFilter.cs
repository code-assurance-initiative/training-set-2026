namespace HarbourLane.Bookings.Rooms;

public sealed record RoomFilter(string? Text, IReadOnlySet<Amenity> Amenities, int MinimumCapacity)
{
    public static RoomFilter None { get; } = new(null, new HashSet<Amenity>(), 0);

    public IReadOnlyList<Room> Apply(IEnumerable<Room> rooms)
    {
        ArgumentNullException.ThrowIfNull(rooms);
        return rooms.Where(Matches).OrderBy(r => r.Name, StringComparer.CurrentCulture).ToList();
    }

    private bool Matches(Room room) =>
        room.Capacity >= MinimumCapacity
        && Amenities.All(room.Amenities.Contains)
        && (string.IsNullOrWhiteSpace(Text)
            || room.Name.Contains(Text.Trim(), StringComparison.CurrentCultureIgnoreCase)
            || room.Floor.Contains(Text.Trim(), StringComparison.CurrentCultureIgnoreCase));
}
