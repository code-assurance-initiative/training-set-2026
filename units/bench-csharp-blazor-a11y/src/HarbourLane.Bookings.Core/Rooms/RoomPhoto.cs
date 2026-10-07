namespace HarbourLane.Bookings.Rooms;

/// <summary>A photo of a room. <see cref="Caption"/> describes what the photo shows.</summary>
public sealed record RoomPhoto(string Url, string Caption);
