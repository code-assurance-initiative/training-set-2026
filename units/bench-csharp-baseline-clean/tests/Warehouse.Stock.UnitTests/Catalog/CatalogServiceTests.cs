using Warehouse.Stock.Application.Catalog;
using Warehouse.Stock.Application.Common;
using Warehouse.Stock.UnitTests.TestSupport;
using static Warehouse.Stock.UnitTests.TestSupport.InventoryTestContext;

namespace Warehouse.Stock.UnitTests.Catalog;

public sealed class CatalogServiceTests
{
    private readonly CatalogService _catalog = new InventoryTestContext().Catalog;

    [Fact]
    public async Task ARegisteredSkuCanBeReadBack()
    {
        var sku = new Sku("WASHER-M8", "Flat washer M8", "EA");

        await _catalog.RegisterSkuAsync(sku, Token);

        Assert.Equal(sku, Value(await _catalog.GetSkuAsync("WASHER-M8", Token)));
    }

    [Fact]
    public async Task RegisteringASkuTwiceIsAConflict()
    {
        var sku = new Sku("WASHER-M8", "Flat washer M8", "EA");
        await _catalog.RegisterSkuAsync(sku, Token);

        var second = await _catalog.RegisterSkuAsync(sku with { Description = "Other" }, Token);

        Assert.Equal(ErrorKind.Conflict, Error(second).Kind);
        Assert.Equal("Flat washer M8", Value(await _catalog.GetSkuAsync("WASHER-M8", Token)).Description);
    }

    [Fact]
    public async Task AnUnknownSkuIsNotFound()
    {
        var result = await _catalog.GetSkuAsync("NOPE-1", Token);

        Assert.Equal(ErrorKind.NotFound, Error(result).Kind);
    }

    [Fact]
    public async Task RegisteringABinTwiceIsAConflict()
    {
        var bin = new BinLocation("C03-01-01", "COLD", 40);
        await _catalog.RegisterBinAsync(bin, Token);

        var second = await _catalog.RegisterBinAsync(bin, Token);

        Assert.Equal(ErrorKind.Conflict, Error(second).Kind);
    }

    [Fact]
    public async Task SkusAreListedInCodeOrderOnePageAtATime()
    {
        foreach (var code in new[] { "NUT-M8", "BOLT-M8-40", "WASHER-M8" })
        {
            await _catalog.RegisterSkuAsync(new Sku(code, code, "EA"), Token);
        }

        var page = await _catalog.ListSkusAsync(new PageRequest(Number: 2, Size: 2), Token);

        Assert.Equal(3, page.TotalCount);
        Assert.Equal(["WASHER-M8"], page.Items.Select(sku => sku.Code));
    }

    [Fact]
    public async Task ResolvingALocationRequiresBothTheSkuAndTheBin()
    {
        var context = await new InventoryTestContext().WithStockAsync(onHand: 0);

        var unknownSku = await context.Catalog.ResolveLocationAsync("NOPE-1", BinCode, Token);
        var unknownBin = await context.Catalog.ResolveLocationAsync(SkuCode, "Z99-99-99", Token);
        var known = await context.Catalog.ResolveLocationAsync(SkuCode, BinCode, Token);

        Assert.Contains("SKU", Error(unknownSku).Message, StringComparison.Ordinal);
        Assert.Contains("Bin", Error(unknownBin).Message, StringComparison.Ordinal);
        Assert.Equal(BinCapacity, Value(known).Capacity);
    }
}
