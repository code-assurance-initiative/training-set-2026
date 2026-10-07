namespace FleetOps.Infrastructure.Parts;

public interface IPartsSupplierClient
{
    Task<IReadOnlyList<Part>> GetCatalogueAsync(CancellationToken cancellationToken);

    Task<PartQuote> QuoteAsync(PartNumber number, CancellationToken cancellationToken);

    Task<PartOrder> OrderAsync(IReadOnlyList<PartOrderLine> lines, CancellationToken cancellationToken);
}
