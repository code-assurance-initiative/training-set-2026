using Warehouse.Stock.Application.Catalog;
using Warehouse.Stock.Application.Common;
using Warehouse.Stock.Infrastructure.InMemory;
using Warehouse.Stock.UnitTests.TestSupport;

namespace Warehouse.Stock.UnitTests.InMemory;

public sealed class InMemoryCatalogStoreTests
{
    private readonly InMemoryCatalogStore _store = new();

    [Fact]
    public async Task ACodeCanBeAddedOnlyOnce()
    {
        var bin = new BinLocation("A01-01-01", "BULK", 10);

        var first = await _store.TryAddBinAsync(bin, InventoryTestContext.Token);
        var second = await _store.TryAddBinAsync(bin with { Capacity = 20 }, InventoryTestContext.Token);

        Assert.True(first);
        Assert.False(second);
        Assert.Equal(10, (await _store.FindBinAsync("A01-01-01", InventoryTestContext.Token))?.Capacity);
    }

    [Fact]
    public async Task CodesAreCaseSensitive()
    {
        await _store.TryAddSkuAsync(new Sku("NUT-M8", "Hex nut M8", "EA"), InventoryTestContext.Token);

        Assert.Null(await _store.FindSkuAsync("nut-m8", InventoryTestContext.Token));
    }

    [Fact]
    public async Task APagePastTheEndIsEmptyButReportsTheTotal()
    {
        await _store.TryAddBinAsync(new BinLocation("A01-01-01", "BULK", 10), InventoryTestContext.Token);

        var page = await _store.ListBinsAsync(new PageRequest(Number: 3, Size: 10), InventoryTestContext.Token);

        Assert.Empty(page.Items);
        Assert.Equal(1, page.TotalCount);
    }
}
