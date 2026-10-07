namespace Warehouse.Stock.Application.Inventory;

/// <summary>Identifies the stock of one SKU held in one bin.</summary>
public readonly record struct StockKey(string SkuCode, string BinCode);
