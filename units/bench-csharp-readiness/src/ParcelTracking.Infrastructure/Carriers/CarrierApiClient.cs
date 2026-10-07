using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using ParcelTracking.Core.Carriers;

namespace ParcelTracking.Infrastructure.Carriers;

/// <summary>Client for the carrier aggregator's tracking API (v2).</summary>
public sealed partial class CarrierApiClient(HttpClient httpClient, ILogger<CarrierApiClient> logger) : ICarrierClient
{
    public async Task<IReadOnlyList<CarrierScan>> GetScansAsync(string carrierCode, string trackingNumber, DateTimeOffset since, CancellationToken cancellationToken)
    {
        var uri = $"v2/carriers/{Uri.EscapeDataString(carrierCode)}/parcels/{Uri.EscapeDataString(trackingNumber)}/scans?since={since.UtcDateTime:O}";
        using var response = await httpClient.GetAsync(uri, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            LogUnknownAtCarrier(carrierCode, trackingNumber);
            return [];
        }

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ScanListDto>(cancellationToken);
        return body?.Scans.Select(s => new CarrierScan(s.Code, s.Timestamp, s.Location)).ToList() ?? [];
    }

    public async Task<IReadOnlyList<CarrierInfo>> GetCarriersAsync(CancellationToken cancellationToken)
    {
        var carriers = await httpClient.GetFromJsonAsync<List<CarrierDto>>("v2/carriers", cancellationToken);
        return carriers?.Select(c => new CarrierInfo(c.Code, c.Name, c.TrackingPage)).ToList() ?? [];
    }

    public async Task<byte[]?> GetLabelAsync(string carrierCode, string trackingNumber, CancellationToken cancellationToken)
    {
        var uri = $"v2/carriers/{Uri.EscapeDataString(carrierCode)}/parcels/{Uri.EscapeDataString(trackingNumber)}/label";
        using var response = await httpClient.GetAsync(uri, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning($"Label request to carrier {carrierCode} failed with HTTP {(int)response.StatusCode}");
            return null;
        }

        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Carrier {CarrierCode} does not know parcel {TrackingNumber} yet")]
    private partial void LogUnknownAtCarrier(string carrierCode, string trackingNumber);

    private sealed record ScanListDto(List<ScanDto> Scans);

    private sealed record ScanDto(string Code, DateTimeOffset Timestamp, string? Location);

    private sealed record CarrierDto(string Code, string Name, Uri TrackingPage);
}
