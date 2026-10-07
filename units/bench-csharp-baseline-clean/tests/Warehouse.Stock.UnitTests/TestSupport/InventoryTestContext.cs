using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Warehouse.Stock.Application.Catalog;
using Warehouse.Stock.Application.Common;
using Warehouse.Stock.Application.Inventory;
using Warehouse.Stock.Application.Reservations;
using Warehouse.Stock.Infrastructure.InMemory;

namespace Warehouse.Stock.UnitTests.TestSupport;

/// <summary>The application services wired to fresh in-memory stores and a controllable clock.</summary>
public sealed class InventoryTestContext
{
    public const string SkuCode = "BOLT-M8-40";
    public const string BinCode = "A01-02-03";
    public const int BinCapacity = 100;

    public InventoryTestContext()
    {
        Catalog = new CatalogService(new InMemoryCatalogStore(), NullLogger<CatalogService>.Instance);
        var inventory = new InMemoryInventoryStore();
        Stock = new StockService(Catalog, inventory, NullLogger<StockService>.Instance);
        Reservations = new ReservationService(
            Catalog, inventory, Options.Create(Settings), Clock, NullLogger<ReservationService>.Instance);
    }

    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 3, 2, 8, 0, 0, TimeSpan.Zero));

    public ReservationOptions Settings { get; } = new();

    public CatalogService Catalog { get; }

    public StockService Stock { get; }

    public ReservationService Reservations { get; }

    public static StockKey Key => new(SkuCode, BinCode);

    public static CancellationToken Token => TestContext.Current.CancellationToken;

    /// <summary>Registers the default SKU and bin and receives <paramref name="onHand"/> units into the bin.</summary>
    public async Task<InventoryTestContext> WithStockAsync(int onHand)
    {
        await Catalog.RegisterSkuAsync(new Sku(SkuCode, "Hex bolt M8 x 40", "EA"), Token);
        await Catalog.RegisterBinAsync(new BinLocation(BinCode, "BULK", BinCapacity), Token);
        if (onHand > 0)
        {
            Value(await Stock.ReceiveAsync(Key, onHand, Token));
        }

        return this;
    }

    public async Task<StockLevel> LevelAsync()
    {
        var levels = Value(await Stock.GetLevelsAsync(SkuCode, Token));
        return Assert.Single(levels);
    }

    public static T Value<T>(OperationResult<T> result)
    {
        Assert.True(result.TryGetValue(out var value, out var error), error?.Message);
        return value;
    }

    public static OperationError Error<T>(OperationResult<T> result)
    {
        Assert.False(result.TryGetValue(out _, out var error), "Expected a failure but the operation succeeded.");
        return error;
    }
}
