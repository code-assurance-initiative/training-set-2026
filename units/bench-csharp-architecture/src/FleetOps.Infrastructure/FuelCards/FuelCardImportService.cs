namespace FleetOps.Infrastructure.FuelCards;

/// <summary>Imports every fuel transaction the provider has that is not stored yet.</summary>
public sealed class FuelCardImportService(IFuelCardClient client, IFuelCardTransactionStore store)
{
    public async Task<FuelCardImportResult> ImportAsync(CancellationToken cancellationToken)
    {
        int fetched = 0, imported = 0;
        string? cursor = null;
        do
        {
            var page = await client.GetTransactionsAsync(cursor, cancellationToken).ConfigureAwait(false);
            foreach (var item in page.Items)
            {
                fetched++;
                if (await store.ContainsAsync(item.Reference, cancellationToken).ConfigureAwait(false))
                {
                    continue;
                }

                store.Add(new FuelTransaction
                {
                    Id = Guid.NewGuid(),
                    ProviderReference = item.Reference,
                    Vin = item.Vin,
                    Station = item.Station,
                    Litres = item.Litres,
                    Amount = item.Amount,
                    PurchasedAt = item.PurchasedAt,
                });
                imported++;
            }

            cursor = page.NextCursor;
        }
        while (cursor is not null);

        await store.SaveAsync(cancellationToken).ConfigureAwait(false);
        return new FuelCardImportResult(fetched, imported);
    }
}
