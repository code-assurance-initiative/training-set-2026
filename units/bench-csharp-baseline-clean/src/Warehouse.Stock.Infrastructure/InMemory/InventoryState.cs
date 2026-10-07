using Warehouse.Stock.Application.Inventory;
using Warehouse.Stock.Application.Reservations;

namespace Warehouse.Stock.Infrastructure.InMemory;

/// <summary>
/// The mutable inventory data behind <see cref="InMemoryInventoryStore"/>. Not thread-safe by itself: the store only
/// hands it out while holding its lock, and everything it returns is an immutable snapshot.
/// </summary>
public sealed class InventoryState : IInventoryLedger
{
    private readonly Dictionary<StockKey, int> _onHand = [];
    private readonly Dictionary<Guid, Reservation> _reservations = [];

    public StockLevel LevelOf(StockKey key) => new(key, _onHand.GetValueOrDefault(key), ReservedAt(key));

    public IReadOnlyList<StockLevel> LevelsForSku(string skuCode) =>
        _onHand.Keys
            .Where(key => key.SkuCode == skuCode)
            .OrderBy(key => key.BinCode, StringComparer.Ordinal)
            .Select(LevelOf)
            .ToList();

    public Reservation? FindReservation(Guid id) => _reservations.GetValueOrDefault(id);

    public IReadOnlyList<Reservation> ActiveReservationsDueBy(DateTimeOffset instant) =>
        _reservations.Values.Where(reservation => reservation.IsDueBy(instant)).ToList();

    public void SetOnHand(StockKey key, int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(quantity);
        _onHand[key] = quantity;
    }

    public void SaveReservation(Reservation reservation) => _reservations[reservation.Id] = reservation;

    private int ReservedAt(StockKey key) =>
        _reservations.Values
            .Where(reservation => reservation.IsActive && reservation.Key == key)
            .Sum(reservation => reservation.Quantity);
}
