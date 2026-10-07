using Microsoft.Extensions.Logging;
using Warehouse.Stock.Application.Common;

namespace Warehouse.Stock.Application.Catalog;

public sealed partial class CatalogService(ICatalogStore store, ILogger<CatalogService> logger)
{
    public async Task<OperationResult<Sku>> RegisterSkuAsync(Sku sku, CancellationToken cancellationToken)
    {
        if (!await store.TryAddSkuAsync(sku, cancellationToken).ConfigureAwait(false))
        {
            return OperationError.Conflict($"SKU '{sku.Code}' is already registered.");
        }

        LogSkuRegistered(sku.Code);
        return OperationResult.Success(sku);
    }

    public async Task<OperationResult<Sku>> GetSkuAsync(string code, CancellationToken cancellationToken)
    {
        var sku = await store.FindSkuAsync(code, cancellationToken).ConfigureAwait(false);
        return sku is null
            ? OperationError.NotFound($"SKU '{code}' does not exist.")
            : OperationResult.Success(sku);
    }

    public Task<Page<Sku>> ListSkusAsync(PageRequest page, CancellationToken cancellationToken) =>
        store.ListSkusAsync(page, cancellationToken);

    public async Task<OperationResult<BinLocation>> RegisterBinAsync(BinLocation bin, CancellationToken cancellationToken)
    {
        if (!await store.TryAddBinAsync(bin, cancellationToken).ConfigureAwait(false))
        {
            return OperationError.Conflict($"Bin '{bin.Code}' is already registered.");
        }

        LogBinRegistered(bin.Code, bin.Zone);
        return OperationResult.Success(bin);
    }

    public async Task<OperationResult<BinLocation>> GetBinAsync(string code, CancellationToken cancellationToken)
    {
        var bin = await store.FindBinAsync(code, cancellationToken).ConfigureAwait(false);
        return bin is null
            ? OperationError.NotFound($"Bin '{code}' does not exist.")
            : OperationResult.Success(bin);
    }

    public Task<Page<BinLocation>> ListBinsAsync(PageRequest page, CancellationToken cancellationToken) =>
        store.ListBinsAsync(page, cancellationToken);

    /// <summary>Confirms that a SKU and a bin both exist, and returns the bin (whose capacity callers need).</summary>
    public async Task<OperationResult<BinLocation>> ResolveLocationAsync(
        string skuCode,
        string binCode,
        CancellationToken cancellationToken)
    {
        if (await store.FindSkuAsync(skuCode, cancellationToken).ConfigureAwait(false) is null)
        {
            return OperationError.NotFound($"SKU '{skuCode}' does not exist.");
        }

        return await GetBinAsync(binCode, cancellationToken).ConfigureAwait(false);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Registered SKU {SkuCode}")]
    private partial void LogSkuRegistered(string skuCode);

    [LoggerMessage(Level = LogLevel.Information, Message = "Registered bin {BinCode} in zone {Zone}")]
    private partial void LogBinRegistered(string binCode, string zone);
}
