using Warehouse.Stock.Application.Catalog;

namespace Warehouse.Stock.Api.Contracts;

public sealed record SkuResponse(string Code, string Description, string UnitOfMeasure)
{
    public static SkuResponse From(Sku sku) => new(sku.Code, sku.Description, sku.UnitOfMeasure);
}
