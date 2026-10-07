using ParcelTracking.Core.Carriers;

namespace ParcelTracking.UnitTests.TestSupport;

public sealed class FakeCarrierClient : ICarrierClient
{
    public Dictionary<string, List<CarrierScan>> Scans { get; } = new(StringComparer.Ordinal);

    public List<CarrierInfo> Carriers { get; } = [];

    public int CarrierListCalls { get; private set; }

    public Task<IReadOnlyList<CarrierScan>> GetScansAsync(string carrierCode, string trackingNumber, DateTimeOffset since, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<CarrierScan>>(Scans.TryGetValue(trackingNumber, out var scans) ? scans : []);

    public Task<IReadOnlyList<CarrierInfo>> GetCarriersAsync(CancellationToken cancellationToken)
    {
        CarrierListCalls++;
        return Task.FromResult<IReadOnlyList<CarrierInfo>>(Carriers.ToList());
    }

    public Task<byte[]?> GetLabelAsync(string carrierCode, string trackingNumber, CancellationToken cancellationToken) =>
        Task.FromResult<byte[]?>(null);
}
