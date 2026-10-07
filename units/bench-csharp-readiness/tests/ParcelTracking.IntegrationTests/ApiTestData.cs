using System.Net.Http.Json;
using ParcelTracking.Api.Contracts;

namespace ParcelTracking.IntegrationTests;

internal static class ApiTestData
{
    public const string Read = "parcels:read";
    public const string Write = "parcels:write";
    public const string Reports = "reports:read";

    private static int _sequence;

    public static string NewTrackingNumber() => $"NP{Interlocked.Increment(ref _sequence):D4}{Random.Shared.Next(100000, 999999)}";

    public static async Task<string> RegisterAsync(HttpClient writer, CancellationToken cancellationToken)
    {
        var trackingNumber = NewTrackingNumber();
        var response = await writer.PostAsJsonAsync(
            "/api/shipments",
            new { trackingNumber, carrierCode = "NORDPOST", destinationPostalCode = "0150" },
            cancellationToken);
        response.EnsureSuccessStatusCode();
        var parcel = await response.Content.ReadFromJsonAsync<ParcelResponse>(cancellationToken);
        return parcel?.TrackingNumber ?? throw new InvalidOperationException("No parcel in the registration response.");
    }
}
