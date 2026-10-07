using System.ComponentModel.DataAnnotations;
using Warehouse.Stock.Application.Inventory;
using Warehouse.Stock.Application.Reservations;

namespace Warehouse.Stock.Api.Contracts;

public sealed record CreateReservationRequest
{
    [Required]
    [RegularExpression(CodePatterns.Sku)]
    public string SkuCode { get; init; } = string.Empty;

    [Required]
    [RegularExpression(CodePatterns.Bin)]
    public string BinCode { get; init; } = string.Empty;

    [Range(1, CodePatterns.MaximumQuantity)]
    public int Quantity { get; init; }

    /// <summary>How long to hold the stock, in minutes; the service default when omitted.</summary>
    [Range(1, 10_080)]
    public int? HoldMinutes { get; init; }

    public NewReservation ToNewReservation() =>
        new(new StockKey(SkuCode, BinCode), Quantity, HoldMinutes is { } minutes ? TimeSpan.FromMinutes(minutes) : null);
}
