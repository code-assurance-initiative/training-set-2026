namespace HarbourLane.Bookings.Rooms;

public static class AmenityNames
{
    public static string Display(Amenity amenity) => amenity switch
    {
        Amenity.Projector => "Projector",
        Amenity.Whiteboard => "Whiteboard",
        Amenity.Kitchen => "Kitchen access",
        Amenity.StepFreeAccess => "Step-free access",
        Amenity.HearingLoop => "Hearing loop",
        Amenity.VideoConferencing => "Video conferencing",
        _ => throw new ArgumentOutOfRangeException(nameof(amenity), amenity, "Unknown amenity."),
    };
}
