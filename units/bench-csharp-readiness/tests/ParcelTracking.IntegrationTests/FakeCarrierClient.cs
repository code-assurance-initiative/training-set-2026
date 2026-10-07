using System.Collections.Concurrent;
using ParcelTracking.Core.Carriers;

namespace ParcelTracking.IntegrationTests;

/// <summary>Stands in for the carrier API so no test leaves the process.</summary>
public sealed class FakeCarrierClient : ICarrierClient
{
    public static readonly byte[] LabelPdf = "%PDF-1.7 test label"u8.ToArray();

    public ConcurrentDictionary<string, IReadOnlyList<CarrierScan>> Scans { get; } = new(StringComparer.Ordinal);

    public Task<IReadOnlyList<CarrierScan>> GetScansAsync(string carrierCode, string trackingNumber, DateTimeOffset since, CancellationToken cancellationToken) =>
        Task.FromResult(Scans.TryGetValue(trackingNumber, out var scans) ? scans : []);

    public Task<IReadOnlyList<CarrierInfo>> GetCarriersAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<CarrierInfo>>([new CarrierInfo("NORDPOST", "Nordpost", new Uri("https://nordpost.test/track/{0}"))]);

    public Task<byte[]?> GetLabelAsync(string carrierCode, string trackingNumber, CancellationToken cancellationToken) =>
        Task.FromResult<byte[]?>(LabelPdf);
}
