namespace HarbourLane.Bookings.Rooms;

public interface IRoomCatalog
{
    Task<IReadOnlyList<Room>> GetAllAsync(CancellationToken cancellationToken);

    Task<Room?> FindAsync(Guid id, CancellationToken cancellationToken);

    Task<Room?> FindBySlugAsync(string slug, CancellationToken cancellationToken);

    Task<bool> UpdateAsync(RoomUpdate update, CancellationToken cancellationToken);
}
