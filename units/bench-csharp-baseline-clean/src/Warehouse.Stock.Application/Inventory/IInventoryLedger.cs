using Warehouse.Stock.Application.Reservations;

namespace Warehouse.Stock.Application.Inventory;

/// <summary>A writable view of stock and reservations, used under the store's exclusive lock.</summary>
public interface IInventoryLedger : IInventoryView
{
    void SetOnHand(StockKey key, int quantity);

    void SaveReservation(Reservation reservation);
}
