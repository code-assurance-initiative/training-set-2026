using System.Text;
using FleetOps.Contracts.Inspections;
using FleetOps.Contracts.WorkOrders;
using FleetOps.Infrastructure.Documents;
using FleetOps.Infrastructure.Geocoding;
using FleetOps.Infrastructure.Parts;
using FleetOps.Infrastructure.Storage;
using FleetOps.Infrastructure.Tyres;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace FleetOps.Application.Tests.Infrastructure;

public sealed class CachingAndStorageTests : IDisposable
{
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 5, 4, 9, 0, 0, TimeSpan.Zero));
    private readonly string _root = Path.Combine(Path.GetTempPath(), "fleetops-tests-" + Guid.NewGuid().ToString("N"));

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public void TheTyreCacheKeepsTheCheapestValidQuote()
    {
        var cache = new TyrePriceCache(_clock);
        var size = new TyreSize(205, 55, 16);
        cache.Offer(new TyreQuote("North", size, TyreSeason.Winter, 90m, new DateOnly(2026, 6, 1)));
        cache.Offer(new TyreQuote("South", size, TyreSeason.Winter, 85m, new DateOnly(2026, 6, 1)));
        cache.Offer(new TyreQuote("East", size, TyreSeason.Winter, 95m, new DateOnly(2026, 6, 1)));

        Assert.Equal("South", cache.Best(size, TyreSeason.Winter)!.Supplier);
        Assert.Null(cache.Best(size, TyreSeason.Summer));

        _clock.Advance(TimeSpan.FromDays(60));
        Assert.Null(cache.Best(size, TyreSeason.Winter));
    }

    [Fact]
    public void TheGeocodingCacheAnswersRepeatsAndEmptiesWhenFull()
    {
        var cache = new GeocodingCache();
        cache.Put("A", new GeoPoint(1, 2));
        Assert.True(cache.TryGet("A", out var point));
        Assert.Equal(new GeoPoint(1, 2), point);
        Assert.False(cache.TryGet("B", out _));
        Assert.Equal("HAVNEGADE 1, 1058 KØBENHAVN, DK", new Address("Havnegade 1", "1058", "København", "DK").Normalised);
    }

    [Fact]
    public async Task TheCatalogueSnapshotIsReplacedWhole()
    {
        var snapshot = new PartsCatalogueSnapshot();
        var refresher = new PartsCatalogueRefresher(new FixedCatalogue(), snapshot, _clock);

        Assert.Equal(2, await refresher.RefreshAsync(Ct));
        Assert.Equal(_clock.GetUtcNow(), snapshot.RefreshedAt);
        Assert.Equal("BP-1", Assert.Single(snapshot.Search("pads")).Number.Value);
    }

    [Fact]
    public async Task DocumentsAreStoredUnderTheRootAndOpenedByKey()
    {
        var store = new FileSystemDocumentStore(Options.Create(new StorageOptions { RootDirectory = _root }), _clock);
        using var content = new MemoryStream(Encoding.UTF8.GetBytes("hello"));

        var stored = await store.PutAsync(DocumentKind.Export, "../../escape.csv", content, Ct);
        await using var opened = await store.OpenAsync(stored.Key, Ct);

        Assert.StartsWith("Export/", stored.Key, StringComparison.Ordinal);
        Assert.EndsWith(".csv", stored.Key, StringComparison.Ordinal);
        Assert.Equal(5, stored.Length);
        Assert.NotNull(opened);
        Assert.Null(await store.OpenAsync("Export/missing.csv", Ct));
        await Assert.ThrowsAsync<ArgumentException>(() => store.OpenAsync("../outside.csv", Ct));
    }

    [Fact]
    public void ExportersWriteOneRowPerItem()
    {
        var names = new DocumentNameBuilder(Options.Create(new DocumentOptions()), _clock);
        var workOrder = new WorkOrderSummary(Guid.NewGuid(), Guid.NewGuid(), "Brakes, front", "Approved", [], 236m, _clock.GetUtcNow(), null);
        var inspection = new InspectionSummary(Guid.NewGuid(), Guid.NewGuid(), _clock.GetUtcNow(), false, ["Dangerous: leak"], null);

        var orders = new WorkOrderCsvExporter(names).Export([workOrder], new CsvExportOptions());
        var inspections = new InspectionCsvExporter(names).Export([inspection], new CsvExportOptions { Format = ExportFormat.Tsv, IncludeHeader = false });

        Assert.Equal("fleetops-work-orders-20260504-0900.csv", orders.FileName);
        Assert.Contains("\"Brakes, front\",Approved,236.00", orders.Content, StringComparison.Ordinal);
        Assert.Equal("fleetops-inspections-20260504-0900.tsv", inspections.FileName);
        Assert.Contains("\tno\tDangerous: leak", inspections.Content, StringComparison.Ordinal);
        Assert.DoesNotContain("Inspected", inspections.Content, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private sealed class FixedCatalogue : IPartsSupplierClient
    {
        public Task<IReadOnlyList<Part>> GetCatalogueAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Part>>(
            [
                new Part(new PartNumber("BP-1"), "Brake pads", PartCategory.Brakes),
                new Part(new PartNumber("OF-7"), "Oil filter", PartCategory.Filters),
            ]);

        public Task<PartQuote> QuoteAsync(PartNumber number, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<PartOrder> OrderAsync(IReadOnlyList<PartOrderLine> lines, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
