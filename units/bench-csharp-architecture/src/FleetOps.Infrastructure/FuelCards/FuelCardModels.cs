namespace FleetOps.Infrastructure.FuelCards;

public enum FuelCardStatus
{
    Active,
    Blocked,
    Expired,
}

public sealed record FuelTransactionDto(string Reference, string Vin, string Station, decimal Litres, decimal Amount, DateTimeOffset PurchasedAt);

public sealed record FuelCardTransactionPage(IReadOnlyList<FuelTransactionDto> Items, string? NextCursor);

public sealed record FuelCardAccount(string AccountNumber, int Cards, decimal CreditLimit, FuelCardStatus Status);

public sealed record FuelPrice(string Station, decimal PricePerLitre, DateTimeOffset ObservedAt);

public sealed record FuelCardImportResult(int Fetched, int Imported);
