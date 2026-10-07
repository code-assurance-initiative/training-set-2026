using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Warehouse.Stock.Application.Catalog;
using Warehouse.Stock.Application.Common;
using Warehouse.Stock.Application.Inventory;

namespace Warehouse.Stock.Application.Reservations;

public sealed partial class ReservationService(
    CatalogService catalog,
    IInventoryStore inventory,
    IOptions<ReservationOptions> options,
    TimeProvider clock,
    ILogger<ReservationService> logger)
{
    private readonly ReservationOptions _options = options.Value;

    public async Task<OperationResult<Reservation>> CreateAsync(NewReservation request, CancellationToken cancellationToken)
    {
        if (request.Quantity <= 0)
        {
            return OperationError.Invalid("A reservation must be for a positive quantity.");
        }

        var holdFor = request.HoldFor ?? _options.DefaultHoldTime;
        if (holdFor <= TimeSpan.Zero || holdFor > _options.MaximumHoldTime)
        {
            return OperationError.Invalid(
                $"A reservation may be held for at most {_options.MaximumHoldTime}; {holdFor} was requested.");
        }

        var location = await catalog.ResolveLocationAsync(request.Key.SkuCode, request.Key.BinCode, cancellationToken)
            .ConfigureAwait(false);
        if (!location.TryGetValue(out _, out var error))
        {
            return error;
        }

        var result = await inventory
            .WriteAsync(ledger => Reserve(ledger, request, holdFor), cancellationToken)
            .ConfigureAwait(false);
        if (result.TryGetValue(out var reservation, out _))
        {
            LogReserved(reservation.Id, reservation.Quantity, reservation.Key.SkuCode, reservation.Key.BinCode);
        }

        return result;
    }

    public async Task<OperationResult<Reservation>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var reservation = await inventory
            .ReadAsync(view => view.FindReservation(id), cancellationToken)
            .ConfigureAwait(false);
        return reservation is null ? NotFound(id) : OperationResult.Success(reservation);
    }

    /// <summary>Cancels an active reservation and returns its quantity to available stock.</summary>
    public Task<OperationResult<Reservation>> ReleaseAsync(Guid id, CancellationToken cancellationToken) =>
        SettleAsync(id, ReservationStatus.Released, cancellationToken);

    /// <summary>Picks the reserved quantity: it leaves the bin, and the reservation is closed.</summary>
    public Task<OperationResult<Reservation>> FulfilAsync(Guid id, CancellationToken cancellationToken) =>
        SettleAsync(id, ReservationStatus.Fulfilled, cancellationToken);

    /// <summary>Marks every active reservation whose hold has lapsed as expired. Safe to run repeatedly.</summary>
    public async Task<int> ExpireDueAsync(CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var expired = await inventory
            .WriteAsync(ledger => ExpireDue(ledger, now), cancellationToken)
            .ConfigureAwait(false);
        if (expired > 0)
        {
            LogExpired(expired);
        }

        return expired;
    }

    private OperationResult<Reservation> Reserve(IInventoryLedger ledger, NewReservation request, TimeSpan holdFor)
    {
        var now = clock.GetUtcNow();
        ExpireDue(ledger, now);

        var available = ledger.LevelOf(request.Key).Available;
        if (available < request.Quantity)
        {
            return OperationError.Conflict(
                $"Only {available} units of '{request.Key.SkuCode}' are available in bin '{request.Key.BinCode}'.");
        }

        var reservation = new Reservation(
            Guid.NewGuid(), request.Key, request.Quantity, now, now + holdFor, ReservationStatus.Active);
        ledger.SaveReservation(reservation);
        return OperationResult.Success(reservation);
    }

    private async Task<OperationResult<Reservation>> SettleAsync(
        Guid id,
        ReservationStatus outcome,
        CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var result = await inventory
            .WriteAsync(ledger => Settle(ledger, id, outcome, now), cancellationToken)
            .ConfigureAwait(false);
        if (result.TryGetValue(out var reservation, out _))
        {
            LogSettled(reservation.Id, reservation.Status);
        }

        return result;
    }

    private static OperationResult<Reservation> Settle(
        IInventoryLedger ledger,
        Guid id,
        ReservationStatus outcome,
        DateTimeOffset now)
    {
        var reservation = ledger.FindReservation(id);
        if (reservation is null)
        {
            return NotFound(id);
        }

        if (!reservation.IsActive || reservation.IsDueBy(now))
        {
            return OperationError.Conflict($"Reservation {id} is no longer active.");
        }

        if (outcome == ReservationStatus.Fulfilled)
        {
            var level = ledger.LevelOf(reservation.Key);
            ledger.SetOnHand(reservation.Key, level.OnHand - reservation.Quantity);
        }

        var settled = reservation.WithStatus(outcome);
        ledger.SaveReservation(settled);
        return OperationResult.Success(settled);
    }

    private static int ExpireDue(IInventoryLedger ledger, DateTimeOffset now)
    {
        var due = ledger.ActiveReservationsDueBy(now);
        foreach (var reservation in due)
        {
            ledger.SaveReservation(reservation.WithStatus(ReservationStatus.Expired));
        }

        return due.Count;
    }

    private static OperationError NotFound(Guid id) => OperationError.NotFound($"Reservation {id} does not exist.");

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Reserved {Quantity} of SKU {SkuCode} in bin {BinCode} as reservation {ReservationId}")]
    private partial void LogReserved(Guid reservationId, int quantity, string skuCode, string binCode);

    [LoggerMessage(Level = LogLevel.Information, Message = "Reservation {ReservationId} is now {Status}")]
    private partial void LogSettled(Guid reservationId, ReservationStatus status);

    [LoggerMessage(Level = LogLevel.Information, Message = "Expired {Count} lapsed reservations")]
    private partial void LogExpired(int count);
}
