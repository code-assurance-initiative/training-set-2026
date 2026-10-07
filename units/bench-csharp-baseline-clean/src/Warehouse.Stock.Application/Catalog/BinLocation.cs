namespace Warehouse.Stock.Application.Catalog;

/// <summary>A physical storage location (aisle, rack, shelf) inside a warehouse zone.</summary>
public sealed record BinLocation(string Code, string Zone, int Capacity);
