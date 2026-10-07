namespace FleetOps.Infrastructure.Tyres;

public enum TyreSeason
{
    Summer,
    Winter,
    AllSeason,
}

public enum TyreOrderStatus
{
    Placed,
    Confirmed,
    Delivered,
    Cancelled,
}

public sealed record TyreQuoteRequest(TyreSize Size, TyreSeason Season, int Quantity);

public sealed record TyreQuote(string Supplier, TyreSize Size, TyreSeason Season, decimal UnitPrice, DateOnly ValidUntil);

public sealed record TyreOrderRequest(string Vin, TyreQuote Quote, int Quantity);

public sealed record TyreOrder(string OrderNumber, string Vin, int Quantity, TyreOrderStatus Status);
