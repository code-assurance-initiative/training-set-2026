using Warehouse.Stock.Application.Common;
using Warehouse.Stock.Application.Reservations;
using Warehouse.Stock.UnitTests.TestSupport;
using static Warehouse.Stock.UnitTests.TestSupport.InventoryTestContext;

namespace Warehouse.Stock.UnitTests.Inventory;

public sealed class StockServiceTests
{
    [Fact]
    public async Task ReceiptsAddToTheQuantityOnHand()
    {
        var context = await new InventoryTestContext().WithStockAsync(onHand: 30);

        var level = Value(await context.Stock.ReceiveAsync(Key, 12, Token));

        Assert.Equal(42, level.OnHand);
        Assert.Equal(42, level.Available);
    }

    [Fact]
    public async Task AReceiptMayNotOverfillTheBin()
    {
        var context = await new InventoryTestContext().WithStockAsync(onHand: 95);

        var result = await context.Stock.ReceiveAsync(Key, 6, Token);

        Assert.Equal(ErrorKind.Conflict, Error(result).Kind);
        Assert.Equal(95, (await context.LevelAsync()).OnHand);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task AReceiptMustBeForAPositiveQuantity(int quantity)
    {
        var context = await new InventoryTestContext().WithStockAsync(onHand: 10);

        var result = await context.Stock.ReceiveAsync(Key, quantity, Token);

        Assert.Equal(ErrorKind.Invalid, Error(result).Kind);
    }

    [Fact]
    public async Task AReceiptIntoAnUnknownBinIsNotFound()
    {
        var context = await new InventoryTestContext().WithStockAsync(onHand: 0);

        var result = await context.Stock.ReceiveAsync(Key with { BinCode = "Z99-99-99" }, 5, Token);

        Assert.Equal(ErrorKind.NotFound, Error(result).Kind);
    }

    [Fact]
    public async Task ACountReplacesTheQuantityOnHand()
    {
        var context = await new InventoryTestContext().WithStockAsync(onHand: 30);

        var level = Value(await context.Stock.CountAsync(Key, 27, Token));

        Assert.Equal(27, level.OnHand);
    }

    [Fact]
    public async Task ACountMayNotFallBelowTheReservedQuantity()
    {
        var context = await new InventoryTestContext().WithStockAsync(onHand: 30);
        Value(await context.Reservations.CreateAsync(new NewReservation(Key, 20, HoldFor: null), Token));

        var result = await context.Stock.CountAsync(Key, 19, Token);

        Assert.Equal(ErrorKind.Conflict, Error(result).Kind);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(BinCapacity + 1)]
    public async Task ACountMustFitTheBin(int counted)
    {
        var context = await new InventoryTestContext().WithStockAsync(onHand: 30);

        var result = await context.Stock.CountAsync(Key, counted, Token);

        Assert.Equal(ErrorKind.Invalid, Error(result).Kind);
    }

    [Fact]
    public async Task LevelsOfAnUnknownSkuAreNotFound()
    {
        var context = new InventoryTestContext();

        var result = await context.Stock.GetLevelsAsync("NOPE-1", Token);

        Assert.Equal(ErrorKind.NotFound, Error(result).Kind);
    }
}
