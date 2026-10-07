namespace FleetOps.Infrastructure.FuelCards;

public interface IFuelCardClient
{
    Task<FuelCardTransactionPage> GetTransactionsAsync(string? cursor, CancellationToken cancellationToken);

    Task<FuelCardAccount> GetAccountAsync(CancellationToken cancellationToken);
}
