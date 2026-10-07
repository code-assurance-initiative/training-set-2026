using Warehouse.Stock.Application.Common;
using Warehouse.Stock.Application.Reservations;
using Warehouse.Stock.UnitTests.TestSupport;
using static Warehouse.Stock.UnitTests.TestSupport.InventoryTestContext;

namespace Warehouse.Stock.UnitTests.Reservations;

public sealed class ReservationServiceTests
{
    [Fact]
    public async Task AReservationHoldsStockForTheDefaultTime()
    {
        var context = await new InventoryTestContext().WithStockAsync(onHand: 50);

        var reservation = Value(await context.Reservations.CreateAsync(new NewReservation(Key, 20, null), Token));

        Assert.Equal(ReservationStatus.Active, reservation.Status);
        Assert.Equal(context.Clock.GetUtcNow() + context.Settings.DefaultHoldTime, reservation.ExpiresAt);
        var level = await context.LevelAsync();
        Assert.Equal((50, 20, 30), (level.OnHand, level.Reserved, level.Available));
    }

    [Fact]
    public async Task StockCannotBeReservedTwice()
    {
        var context = await new InventoryTestContext().WithStockAsync(onHand: 50);
        Value(await context.Reservations.CreateAsync(new NewReservation(Key, 40, null), Token));

        var second = await context.Reservations.CreateAsync(new NewReservation(Key, 11, null), Token);

        Assert.Equal(ErrorKind.Conflict, Error(second).Kind);
    }

    [Fact]
    public async Task AHoldLongerThanTheMaximumIsRejected()
    {
        var context = await new InventoryTestContext().WithStockAsync(onHand: 50);
        var tooLong = context.Settings.MaximumHoldTime + TimeSpan.FromMinutes(1);

        var result = await context.Reservations.CreateAsync(new NewReservation(Key, 1, tooLong), Token);

        Assert.Equal(ErrorKind.Invalid, Error(result).Kind);
    }

    [Fact]
    public async Task AReservationMustBeForAPositiveQuantity()
    {
        var context = await new InventoryTestContext().WithStockAsync(onHand: 50);

        var result = await context.Reservations.CreateAsync(new NewReservation(Key, 0, null), Token);

        Assert.Equal(ErrorKind.Invalid, Error(result).Kind);
    }

    [Fact]
    public async Task ReleasingReturnsTheQuantityToAvailableStock()
    {
        var context = await new InventoryTestContext().WithStockAsync(onHand: 50);
        var reservation = Value(await context.Reservations.CreateAsync(new NewReservation(Key, 20, null), Token));

        var released = Value(await context.Reservations.ReleaseAsync(reservation.Id, Token));

        Assert.Equal(ReservationStatus.Released, released.Status);
        Assert.Equal(50, (await context.LevelAsync()).Available);
    }

    [Fact]
    public async Task FulfillingTakesTheQuantityOutOfTheBin()
    {
        var context = await new InventoryTestContext().WithStockAsync(onHand: 50);
        var reservation = Value(await context.Reservations.CreateAsync(new NewReservation(Key, 20, null), Token));

        var fulfilled = Value(await context.Reservations.FulfilAsync(reservation.Id, Token));

        Assert.Equal(ReservationStatus.Fulfilled, fulfilled.Status);
        var level = await context.LevelAsync();
        Assert.Equal((30, 0), (level.OnHand, level.Reserved));
    }

    [Fact]
    public async Task ASettledReservationCannotBeSettledAgain()
    {
        var context = await new InventoryTestContext().WithStockAsync(onHand: 50);
        var reservation = Value(await context.Reservations.CreateAsync(new NewReservation(Key, 20, null), Token));
        Value(await context.Reservations.FulfilAsync(reservation.Id, Token));

        var again = await context.Reservations.ReleaseAsync(reservation.Id, Token);

        Assert.Equal(ErrorKind.Conflict, Error(again).Kind);
        Assert.Equal(30, (await context.LevelAsync()).OnHand);
    }

    [Fact]
    public async Task ALapsedReservationCannotBeFulfilled()
    {
        var context = await new InventoryTestContext().WithStockAsync(onHand: 50);
        var hold = TimeSpan.FromMinutes(5);
        var reservation = Value(await context.Reservations.CreateAsync(new NewReservation(Key, 20, hold), Token));

        context.Clock.Advance(hold);
        var result = await context.Reservations.FulfilAsync(reservation.Id, Token);

        Assert.Equal(ErrorKind.Conflict, Error(result).Kind);
        Assert.Equal(50, (await context.LevelAsync()).OnHand);
    }

    [Fact]
    public async Task ExpiryLapsesOnlyReservationsPastTheirHoldAndIsIdempotent()
    {
        var context = await new InventoryTestContext().WithStockAsync(onHand: 50);
        var shortHold = Value(await context.Reservations.CreateAsync(
            new NewReservation(Key, 10, TimeSpan.FromMinutes(5)), Token));
        var longHold = Value(await context.Reservations.CreateAsync(
            new NewReservation(Key, 10, TimeSpan.FromHours(1)), Token));

        context.Clock.Advance(TimeSpan.FromMinutes(10));
        var first = await context.Reservations.ExpireDueAsync(Token);
        var second = await context.Reservations.ExpireDueAsync(Token);

        Assert.Equal((1, 0), (first, second));
        Assert.Equal(ReservationStatus.Expired, Value(await context.Reservations.GetAsync(shortHold.Id, Token)).Status);
        Assert.Equal(ReservationStatus.Active, Value(await context.Reservations.GetAsync(longHold.Id, Token)).Status);
        Assert.Equal(40, (await context.LevelAsync()).Available);
    }

    [Fact]
    public async Task ANewReservationCanUseStockWhoseHoldHasLapsedBeforeTheSweep()
    {
        var context = await new InventoryTestContext().WithStockAsync(onHand: 50);
        Value(await context.Reservations.CreateAsync(new NewReservation(Key, 50, TimeSpan.FromMinutes(5)), Token));

        context.Clock.Advance(TimeSpan.FromMinutes(6));
        var result = await context.Reservations.CreateAsync(new NewReservation(Key, 50, null), Token);

        Assert.Equal(50, Value(result).Quantity);
    }

    [Fact]
    public async Task AnUnknownReservationIsNotFound()
    {
        var context = new InventoryTestContext();

        var result = await context.Reservations.GetAsync(Guid.NewGuid(), Token);

        Assert.Equal(ErrorKind.NotFound, Error(result).Kind);
    }
}
