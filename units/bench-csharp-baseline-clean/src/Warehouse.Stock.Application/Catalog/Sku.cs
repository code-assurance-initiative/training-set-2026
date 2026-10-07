namespace Warehouse.Stock.Application.Catalog;

/// <summary>A stock-keeping unit: one kind of item the warehouse holds.</summary>
public sealed record Sku(string Code, string Description, string UnitOfMeasure);
