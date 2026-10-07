using Microsoft.Extensions.Logging;
using Warehouse.Stock.Application.Catalog;
using Warehouse.Stock.Application.Common;

namespace Warehouse.Stock.Application.Inventory;

public sealed partial class StockService(
    CatalogService catalog,
    IInventoryStore inventory,
    ILogger<StockService> logger)
{
    public async Task<OperationResult<IReadOnlyList<StockLevel>>> GetLevelsAsync(
        string skuCode,
        CancellationToken cancellationToken)
    {
        var sku = await catalog.GetSkuAsync(skuCode, cancellationToken).ConfigureAwait(false);
        if (!sku.TryGetValue(out _, out var error))
        {
            return error;
        }

        var levels = await inventory
            .ReadAsync(view => view.LevelsForSku(skuCode), cancellationToken)
            .ConfigureAwait(false);
        return OperationResult.Success(levels);
    }

    /// <summary>Books goods received into a bin, up to the bin's capacity.</summary>
    public async Task<OperationResult<StockLevel>> ReceiveAsync(
        StockKey key,
        int quantity,
        CancellationToken cancellationToken)
    {
        if (quantity <= 0)
        {
            return OperationError.Invalid("A receipt must be for a positive quantity.");
        }

        var location = await catalog.ResolveLocationAsync(key.SkuCode, key.BinCode, cancellationToken)
            .ConfigureAwait(false);
        if (!location.TryGetValue(out var bin, out var error))
        {
            return error;
        }

        var result = await inventory
            .WriteAsync(ledger => Receive(ledger, key, quantity, bin.Capacity), cancellationToken)
            .ConfigureAwait(false);
        LogIfApplied(result, "receipt", key, quantity);
        return result;
    }

    /// <summary>Replaces the on-hand quantity with a physical count. The count may not drop below what is reserved.</summary>
    public async Task<OperationResult<StockLevel>> CountAsync(
        StockKey key,
        int countedQuantity,
        CancellationToken cancellationToken)
    {
        if (countedQuantity < 0)
        {
            return OperationError.Invalid("A count cannot be negative.");
        }

        var location = await catalog.ResolveLocationAsync(key.SkuCode, key.BinCode, cancellationToken)
            .ConfigureAwait(false);
        if (!location.TryGetValue(out var bin, out var error))
        {
            return error;
        }

        var result = await inventory
            .WriteAsync(ledger => Count(ledger, key, countedQuantity, bin.Capacity), cancellationToken)
            .ConfigureAwait(false);
        LogIfApplied(result, "count", key, countedQuantity);
        return result;
    }

    private static OperationResult<StockLevel> Receive(IInventoryLedger ledger, StockKey key, int quantity, int capacity)
    {
        var level = ledger.LevelOf(key);
        if (level.OnHand + quantity > capacity)
        {
            return OperationError.Conflict(
                $"Receiving {quantity} would exceed the capacity ({capacity}) of bin '{key.BinCode}'.");
        }

        ledger.SetOnHand(key, level.OnHand + quantity);
        return OperationResult.Success(ledger.LevelOf(key));
    }

    private static OperationResult<StockLevel> Count(IInventoryLedger ledger, StockKey key, int counted, int capacity)
    {
        var level = ledger.LevelOf(key);
        if (counted > capacity)
        {
            return OperationError.Invalid($"A count of {counted} exceeds the capacity ({capacity}) of bin '{key.BinCode}'.");
        }

        if (counted < level.Reserved)
        {
            return OperationError.Conflict(
                $"A count of {counted} is below the {level.Reserved} units reserved in bin '{key.BinCode}'.");
        }

        ledger.SetOnHand(key, counted);
        return OperationResult.Success(ledger.LevelOf(key));
    }

    private void LogIfApplied(OperationResult<StockLevel> result, string movement, StockKey key, int quantity)
    {
        if (result.TryGetValue(out var level, out _))
        {
            LogStockMovement(movement, key.SkuCode, key.BinCode, quantity, level.OnHand);
        }
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Applied stock {Movement} of {Quantity} for SKU {SkuCode} in bin {BinCode}; on hand now {OnHand}")]
    private partial void LogStockMovement(string movement, string skuCode, string binCode, int quantity, int onHand);
}
