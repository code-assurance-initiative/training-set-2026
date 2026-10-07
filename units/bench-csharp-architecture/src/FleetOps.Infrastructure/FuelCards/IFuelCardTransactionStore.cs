namespace FleetOps.Infrastructure.FuelCards;

public interface IFuelCardTransactionStore
{
    Task<bool> ContainsAsync(string providerReference, CancellationToken cancellationToken);

    void Add(FuelTransaction transaction);

    Task<FuelPrice?> LatestPriceAsync(string station, CancellationToken cancellationToken);

    Task SaveAsync(CancellationToken cancellationToken);
}
