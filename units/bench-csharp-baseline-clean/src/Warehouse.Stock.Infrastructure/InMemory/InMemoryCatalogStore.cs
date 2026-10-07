using Warehouse.Stock.Application.Catalog;
using Warehouse.Stock.Application.Common;

namespace Warehouse.Stock.Infrastructure.InMemory;

/// <summary>Thread-safe in-process catalogue. Listings are ordered by code so that paging is stable.</summary>
public sealed class InMemoryCatalogStore : ICatalogStore
{
    private readonly Lock _gate = new();
    private readonly SortedDictionary<string, Sku> _skus = new(StringComparer.Ordinal);
    private readonly SortedDictionary<string, BinLocation> _bins = new(StringComparer.Ordinal);

    public Task<bool> TryAddSkuAsync(Sku sku, CancellationToken cancellationToken) =>
        Task.FromResult(TryAdd(_skus, sku.Code, sku, cancellationToken));

    public Task<Sku?> FindSkuAsync(string code, CancellationToken cancellationToken) =>
        Task.FromResult(Find(_skus, code, cancellationToken));

    public Task<Page<Sku>> ListSkusAsync(PageRequest page, CancellationToken cancellationToken) =>
        Task.FromResult(List(_skus, page, cancellationToken));

    public Task<bool> TryAddBinAsync(BinLocation bin, CancellationToken cancellationToken) =>
        Task.FromResult(TryAdd(_bins, bin.Code, bin, cancellationToken));

    public Task<BinLocation?> FindBinAsync(string code, CancellationToken cancellationToken) =>
        Task.FromResult(Find(_bins, code, cancellationToken));

    public Task<Page<BinLocation>> ListBinsAsync(PageRequest page, CancellationToken cancellationToken) =>
        Task.FromResult(List(_bins, page, cancellationToken));

    private bool TryAdd<T>(SortedDictionary<string, T> items, string code, T item, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return items.TryAdd(code, item);
        }
    }

    private T? Find<T>(SortedDictionary<string, T> items, string code, CancellationToken cancellationToken)
        where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return items.GetValueOrDefault(code);
        }
    }

    private Page<T> List<T>(SortedDictionary<string, T> items, PageRequest page, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            var slice = items.Values.Skip(page.Skip).Take(page.Size).ToList();
            return new Page<T>(slice, page.Number, page.Size, items.Count);
        }
    }
}
