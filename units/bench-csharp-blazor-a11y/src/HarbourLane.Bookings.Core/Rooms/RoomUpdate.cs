namespace HarbourLane.Bookings.Rooms;

public sealed record RoomUpdate(Guid Id, string Name, int Capacity, decimal HourlyRate, string DescriptionHtml);
