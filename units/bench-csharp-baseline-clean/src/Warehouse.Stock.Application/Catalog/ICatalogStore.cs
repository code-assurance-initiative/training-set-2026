using Warehouse.Stock.Application.Common;

namespace Warehouse.Stock.Application.Catalog;

/// <summary>Persistence port for the SKU and bin catalogue.</summary>
public interface ICatalogStore
{
    Task<bool> TryAddSkuAsync(Sku sku, CancellationToken cancellationToken);

    Task<Sku?> FindSkuAsync(string code, CancellationToken cancellationToken);

    Task<Page<Sku>> ListSkusAsync(PageRequest page, CancellationToken cancellationToken);

    Task<bool> TryAddBinAsync(BinLocation bin, CancellationToken cancellationToken);

    Task<BinLocation?> FindBinAsync(string code, CancellationToken cancellationToken);

    Task<Page<BinLocation>> ListBinsAsync(PageRequest page, CancellationToken cancellationToken);
}
