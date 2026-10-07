namespace HarbourLane.Bookings.Rooms;

public sealed record Room
{
    public required Guid Id { get; init; }

    public required string Slug { get; init; }

    public required string Name { get; init; }

    public required string Floor { get; init; }

    public required int Capacity { get; init; }

    public required decimal HourlyRate { get; init; }

    public required IReadOnlySet<Amenity> Amenities { get; init; }

    /// <summary>Formatted description written by staff in the admin area (HTML).</summary>
    public required string DescriptionHtml { get; init; }

    public required IReadOnlyList<RoomPhoto> Photos { get; init; }

    public string? TourVideoUrl { get; init; }

    /// <summary>Identifier of the room's area on the building map.</summary>
    public required string MapAreaId { get; init; }
}
