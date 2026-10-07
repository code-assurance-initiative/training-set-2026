using Warehouse.Stock.Application.Inventory;
using Warehouse.Stock.Infrastructure.InMemory;
using Warehouse.Stock.UnitTests.TestSupport;

namespace Warehouse.Stock.UnitTests.InMemory;

public sealed class InMemoryInventoryStoreTests
{
    private static readonly StockKey Key = InventoryTestContext.Key;

    private readonly InMemoryInventoryStore _store = new();

    [Fact]
    public async Task AWriteIsVisibleToLaterReads()
    {
        await _store.WriteAsync(ledger => Set(ledger, 7), InventoryTestContext.Token);

        var level = await _store.ReadAsync(view => view.LevelOf(Key), InventoryTestContext.Token);

        Assert.Equal(7, level.OnHand);
    }

    [Fact]
    public async Task ConcurrentReadModifyWriteCallbacksDoNotLoseUpdates()
    {
        const int writers = 200;

        await Task.WhenAll(Enumerable.Range(0, writers).Select(_ => Task.Run(
            () => _store.WriteAsync(ledger => Set(ledger, ledger.LevelOf(Key).OnHand + 1), InventoryTestContext.Token),
            InventoryTestContext.Token)));

        var level = await _store.ReadAsync(view => view.LevelOf(Key), InventoryTestContext.Token);
        Assert.Equal(writers, level.OnHand);
    }

    [Fact]
    public async Task ACancelledRequestDoesNotRunItsCallback()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _store.WriteAsync(ledger => Set(ledger, 1), cancelled.Token));

        var level = await _store.ReadAsync(view => view.LevelOf(Key), InventoryTestContext.Token);
        Assert.Equal(0, level.OnHand);
    }

    [Fact]
    public async Task ANegativeQuantityOnHandIsRefused()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _store.WriteAsync(ledger => Set(ledger, -1), InventoryTestContext.Token));
    }

    [Fact]
    public async Task LevelsForASkuAreOrderedByBin()
    {
        await _store.WriteAsync(
            ledger =>
            {
                ledger.SetOnHand(Key with { BinCode = "B02-01-01" }, 2);
                ledger.SetOnHand(Key with { BinCode = "A01-01-01" }, 1);
                ledger.SetOnHand(new StockKey("NUT-M8", "A01-01-01"), 9);
                return true;
            },
            InventoryTestContext.Token);

        var levels = await _store.ReadAsync(view => view.LevelsForSku(Key.SkuCode), InventoryTestContext.Token);

        Assert.Equal(["A01-01-01", "B02-01-01"], levels.Select(level => level.Key.BinCode));
    }

    private static bool Set(IInventoryLedger ledger, int onHand)
    {
        ledger.SetOnHand(Key, onHand);
        return true;
    }
}
