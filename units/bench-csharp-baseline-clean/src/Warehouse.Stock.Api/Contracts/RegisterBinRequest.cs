using System.ComponentModel.DataAnnotations;
using Warehouse.Stock.Application.Catalog;

namespace Warehouse.Stock.Api.Contracts;

public sealed record RegisterBinRequest
{
    [Required]
    [RegularExpression(CodePatterns.Bin)]
    public string Code { get; init; } = string.Empty;

    [Required]
    [RegularExpression(CodePatterns.Zone)]
    public string Zone { get; init; } = string.Empty;

    [Range(1, CodePatterns.MaximumQuantity)]
    public int Capacity { get; init; }

    public BinLocation ToBin() => new(Code, Zone, Capacity);
}
