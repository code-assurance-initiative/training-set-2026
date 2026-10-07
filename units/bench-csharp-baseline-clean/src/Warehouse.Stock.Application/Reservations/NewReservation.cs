using Warehouse.Stock.Application.Inventory;

namespace Warehouse.Stock.Application.Reservations;

/// <param name="HoldFor">How long to hold the stock; the configured default when omitted.</param>
public sealed record NewReservation(StockKey Key, int Quantity, TimeSpan? HoldFor);
