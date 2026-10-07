namespace HarbourLane.Bookings.Rooms;

/// <summary>The centre's rooms as they are on opening day.</summary>
public static class RoomSeed
{
    public static IReadOnlyList<Room> Rooms { get; } =
    [
        new Room
        {
            Id = Guid.Parse("4f1c2b7e-8d0a-4c1e-9a55-0d3c6b1e2f01"),
            Slug = "main-hall",
            Name = "Main hall",
            Floor = "Ground floor",
            Capacity = 120,
            HourlyRate = 45m,
            Amenities = new HashSet<Amenity> { Amenity.Projector, Amenity.StepFreeAccess, Amenity.HearingLoop, Amenity.Kitchen },
            DescriptionHtml = "<p>Sprung wooden floor, stage with steps and a ramp, stackable chairs for 120.</p>",
            Photos =
            [
                new RoomPhoto("https://media.harbourlane.org/rooms/main-hall-1.jpg", "The main hall set out with rows of chairs facing the stage"),
                new RoomPhoto("https://media.harbourlane.org/rooms/main-hall-2.jpg", "The ramp to the stage, beside the steps"),
            ],
            TourVideoUrl = "https://media.harbourlane.org/rooms/main-hall-tour.mp4",
            MapAreaId = "area-main-hall",
        },
        new Room
        {
            Id = Guid.Parse("9b6a1d3c-27e4-4b8f-8c1d-5e2a7f9c3d02"),
            Slug = "harbour-room",
            Name = "Harbour room",
            Floor = "First floor",
            Capacity = 16,
            HourlyRate = 18m,
            Amenities = new HashSet<Amenity> { Amenity.Whiteboard, Amenity.VideoConferencing, Amenity.StepFreeAccess },
            DescriptionHtml = "<p>Meeting table for 16 with a view over the harbour. Lift access from the foyer.</p>",
            Photos =
            [
                new RoomPhoto("https://media.harbourlane.org/rooms/harbour-room-1.jpg", "A long meeting table under a window overlooking the harbour"),
            ],
            TourVideoUrl = null,
            MapAreaId = "area-harbour-room",
        },
        new Room
        {
            Id = Guid.Parse("c3d8e5f1-6a2b-4d7c-b9e0-1f4a8c6d5e03"),
            Slug = "workshop",
            Name = "Workshop",
            Floor = "Ground floor",
            Capacity = 24,
            HourlyRate = 22m,
            Amenities = new HashSet<Amenity> { Amenity.Kitchen, Amenity.Whiteboard },
            DescriptionHtml = "<p>Wipe-clean tables, a sink and a kitchen hatch. Good for craft groups and cooking classes.</p>",
            Photos =
            [
                new RoomPhoto("https://media.harbourlane.org/rooms/workshop-1.jpg", "Workshop tables with a sink and the kitchen hatch behind them"),
            ],
            TourVideoUrl = "https://media.harbourlane.org/rooms/workshop-tour.mp4",
            MapAreaId = "area-workshop",
        },
    ];
}
