using System.Net;
using System.Net.Http.Json;
using FleetOps.Contracts.Vehicles;

namespace FleetOps.Infrastructure.Telematics;

public sealed class TelematicsClient(HttpClient http) : ITelematicsClient
{
    public async Task<OdometerReading> GetOdometerAsync(string vin, CancellationToken cancellationToken)
    {
        var reading = await http
            .GetFromJsonAsync<OdometerReading>(new Uri($"v2/vehicles/{Uri.EscapeDataString(vin)}/odometer", UriKind.Relative), cancellationToken)
            .ConfigureAwait(false);
        return reading ?? throw new HttpRequestException($"The telematics API returned no odometer for {vin}.");
    }

    public async Task<VehiclePosition?> GetPositionAsync(string vin, CancellationToken cancellationToken)
    {
        using var response = await http
            .GetAsync(new Uri($"v2/vehicles/{Uri.EscapeDataString(vin)}/position", UriKind.Relative), cancellationToken)
            .ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<VehiclePosition>(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> PingAsync(CancellationToken cancellationToken)
    {
        using var response = await http
            .GetAsync(new Uri("v2/health", UriKind.Relative), cancellationToken)
            .ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }
}
