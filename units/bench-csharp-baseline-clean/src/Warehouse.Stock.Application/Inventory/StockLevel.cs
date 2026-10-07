namespace Warehouse.Stock.Application.Inventory;

public sealed record StockLevel(StockKey Key, int OnHand, int Reserved)
{
    public int Available => OnHand - Reserved;
}
