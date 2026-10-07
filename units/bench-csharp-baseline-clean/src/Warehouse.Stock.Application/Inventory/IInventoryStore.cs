namespace Warehouse.Stock.Application.Inventory;

/// <summary>
/// Persistence port for stock levels and reservations. Each callback runs in isolation from every other, so a
/// check-then-act sequence (such as "is enough available? then reserve") cannot interleave with another.
/// </summary>
public interface IInventoryStore
{
    Task<T> ReadAsync<T>(Func<IInventoryView, T> query, CancellationToken cancellationToken);

    Task<T> WriteAsync<T>(Func<IInventoryLedger, T> change, CancellationToken cancellationToken);
}
