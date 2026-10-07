namespace FleetOps.Infrastructure.Parts;

public sealed class PartsCatalogueRefresher(IPartsSupplierClient supplier, PartsCatalogueSnapshot snapshot, TimeProvider clock)
{
    public async Task<int> RefreshAsync(CancellationToken cancellationToken)
    {
        var parts = await supplier.GetCatalogueAsync(cancellationToken).ConfigureAwait(false);
        snapshot.Replace(parts, clock.GetUtcNow());
        return parts.Count;
    }
}
