using Warehouse.Stock.Application.Reservations;

namespace Warehouse.Stock.Application.Inventory;

/// <summary>A consistent, read-only view of stock and reservations, valid only inside a store callback.</summary>
public interface IInventoryView
{
    StockLevel LevelOf(StockKey key);

    IReadOnlyList<StockLevel> LevelsForSku(string skuCode);

    Reservation? FindReservation(Guid id);

    IReadOnlyList<Reservation> ActiveReservationsDueBy(DateTimeOffset instant);
}
