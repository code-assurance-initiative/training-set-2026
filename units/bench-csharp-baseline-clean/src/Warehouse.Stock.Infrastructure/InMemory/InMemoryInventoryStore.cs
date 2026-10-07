using Warehouse.Stock.Application.Inventory;

namespace Warehouse.Stock.Infrastructure.InMemory;

/// <summary>
/// In-process inventory store (ADR 0002). One lock serialises every read and write, which makes each callback a
/// consistent unit; the workload of a single warehouse service is far below where that lock would contend.
/// </summary>
public sealed class InMemoryInventoryStore : IInventoryStore
{
    private readonly Lock _gate = new();
    private readonly InventoryState _state = new();

    public Task<T> ReadAsync<T>(Func<IInventoryView, T> query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return Task.FromResult(Run(() => query(_state), cancellationToken));
    }

    public Task<T> WriteAsync<T>(Func<IInventoryLedger, T> change, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(change);
        return Task.FromResult(Run(() => change(_state), cancellationToken));
    }

    private T Run<T>(Func<T> callback, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return callback();
        }
    }
}
