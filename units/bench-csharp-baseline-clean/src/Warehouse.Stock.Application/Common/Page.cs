namespace Warehouse.Stock.Application.Common;

public sealed record Page<T>(IReadOnlyList<T> Items, int Number, int Size, int TotalCount);
