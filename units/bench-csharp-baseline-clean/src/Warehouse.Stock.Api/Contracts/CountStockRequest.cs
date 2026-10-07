using System.ComponentModel.DataAnnotations;

namespace Warehouse.Stock.Api.Contracts;

public sealed record CountStockRequest
{
    [Required]
    [RegularExpression(CodePatterns.Bin)]
    public string BinCode { get; init; } = string.Empty;

    [Range(0, CodePatterns.MaximumQuantity)]
    public int CountedQuantity { get; init; }
}
