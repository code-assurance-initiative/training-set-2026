using Warehouse.Stock.Application.Inventory;

namespace Warehouse.Stock.Api.Contracts;

public sealed record StockLevelResponse(string SkuCode, string BinCode, int OnHand, int Reserved, int Available)
{
    public static StockLevelResponse From(StockLevel level) =>
        new(level.Key.SkuCode, level.Key.BinCode, level.OnHand, level.Reserved, level.Available);
}
