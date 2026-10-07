using System.ComponentModel.DataAnnotations;

namespace Warehouse.Stock.Api.Contracts;

public sealed record ReceiveStockRequest
{
    [Required]
    [RegularExpression(CodePatterns.Bin)]
    public string BinCode { get; init; } = string.Empty;

    [Range(1, CodePatterns.MaximumQuantity)]
    public int Quantity { get; init; }
}
