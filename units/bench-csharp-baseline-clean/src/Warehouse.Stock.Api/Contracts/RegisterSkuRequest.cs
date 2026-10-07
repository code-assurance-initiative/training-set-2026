using System.ComponentModel.DataAnnotations;
using Warehouse.Stock.Application.Catalog;

namespace Warehouse.Stock.Api.Contracts;

public sealed record RegisterSkuRequest
{
    [Required]
    [RegularExpression(CodePatterns.Sku)]
    public string Code { get; init; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Description { get; init; } = string.Empty;

    [Required]
    [RegularExpression(CodePatterns.UnitOfMeasure)]
    public string UnitOfMeasure { get; init; } = string.Empty;

    public Sku ToSku() => new(Code, Description, UnitOfMeasure);
}
