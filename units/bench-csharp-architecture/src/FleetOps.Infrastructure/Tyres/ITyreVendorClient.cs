namespace FleetOps.Infrastructure.Tyres;

public interface ITyreVendorClient
{
    Task<IReadOnlyList<TyreQuote>> QuoteAsync(TyreQuoteRequest request, CancellationToken cancellationToken);

    Task<TyreOrder> OrderAsync(TyreOrderRequest request, CancellationToken cancellationToken);
}
