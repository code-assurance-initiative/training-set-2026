using System.Collections.Concurrent;

namespace HarbourLane.Bookings.Rooms;

public sealed class InMemoryRoomCatalog : IRoomCatalog
{
    private readonly ConcurrentDictionary<Guid, Room> _rooms;

    public InMemoryRoomCatalog(IEnumerable<Room> rooms)
    {
        ArgumentNullException.ThrowIfNull(rooms);
        _rooms = new ConcurrentDictionary<Guid, Room>(rooms.ToDictionary(r => r.Id));
    }

    public Task<IReadOnlyList<Room>> GetAllAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<Room> rooms = [.. _rooms.Values.OrderBy(r => r.Name, StringComparer.Ordinal)];
        return Task.FromResult(rooms);
    }

    public Task<Room?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_rooms.GetValueOrDefault(id));
    }

    public Task<Room?> FindBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var room = _rooms.Values.FirstOrDefault(r => string.Equals(r.Slug, slug, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(room);
    }

    public Task<bool> UpdateAsync(RoomUpdate update, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(update);
        cancellationToken.ThrowIfCancellationRequested();
        while (_rooms.TryGetValue(update.Id, out var current))
        {
            var updated = current with
            {
                Name = update.Name,
                Capacity = update.Capacity,
                HourlyRate = update.HourlyRate,
                DescriptionHtml = update.DescriptionHtml,
            };
            if (_rooms.TryUpdate(update.Id, updated, current))
            {
                return Task.FromResult(true);
            }
        }

        return Task.FromResult(false);
    }
}
