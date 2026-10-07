using BenchmarkDotNet.Attributes;
using ParcelTracking.Core.Carriers;
using ParcelTracking.Core.Parcels;

namespace ParcelTracking.Benchmarks;

/// <summary>
/// Status normalisation runs once per carrier scan, for every active parcel on every polling round, so it is the
/// worker's hot path. Run with: dotnet run -c Release --project benchmarks/ParcelTracking.Benchmarks
/// </summary>
[MemoryDiagnoser]
public class StatusMappingBenchmarks
{
    private readonly CarrierStatusMap _map = CarrierStatusMap.Default;
    private readonly (string Carrier, string Code)[] _scans =
    [
        ("NORDPOST", "HUB"),
        ("nordpost", " dlv "),
        ("SWIFTLINE", "IN_TRANSIT"),
        ("SWIFTLINE", "UNKNOWN_CODE"),
    ];

    [Benchmark(Baseline = true)]
    public int NormaliseKnownAndUnknown()
    {
        var mapped = 0;
        foreach (var (carrier, code) in _scans)
        {
            if (_map.TryNormalise(carrier, code, out _))
            {
                mapped++;
            }
        }

        return mapped;
    }

    [Benchmark]
    public bool RecordDuplicateScan()
    {
        var parcel = Parcel.Register("NP123456789", "merchant-1", "NORDPOST", "0150", DateTimeOffset.UnixEpoch);
        var scan = new TrackingEvent(Guid.Empty, parcel.Id, DateTimeOffset.UnixEpoch.AddHours(1), "HUB", ParcelStatus.InTransit, "Oslo");
        parcel.Record(scan);
        return parcel.Record(scan);
    }
}
