using FleetOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FleetOps.Infrastructure.FuelCards;

public sealed class FuelCardTransactionStore(FleetOpsDbContext db) : IFuelCardTransactionStore
{
    public Task<bool> ContainsAsync(string providerReference, CancellationToken cancellationToken) =>
        db.FuelTransactions.AnyAsync(t => t.ProviderReference == providerReference, cancellationToken);

    public void Add(FuelTransaction transaction) => db.FuelTransactions.Add(transaction);

    public async Task<FuelPrice?> LatestPriceAsync(string station, CancellationToken cancellationToken)
    {
        var latest = await db.FuelTransactions.AsNoTracking()
            .Where(t => t.Station == station && t.Litres > 0)
            .OrderByDescending(t => t.PurchasedAt)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        return latest is null ? null : new FuelPrice(station, Math.Round(latest.Amount / latest.Litres, 3), latest.PurchasedAt);
    }

    public Task SaveAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
