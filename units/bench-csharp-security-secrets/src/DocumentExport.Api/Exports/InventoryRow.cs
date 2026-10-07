namespace DocumentExport.Api.Exports;

/// <summary>One stock line of a warehouse.</summary>
public sealed record InventoryRow(string Sku, string Description, int Quantity, string BinLocation);
