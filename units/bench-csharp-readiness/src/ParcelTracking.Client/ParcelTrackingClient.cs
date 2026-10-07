using System.Net;
using System.Net.Http.Json;

namespace ParcelTracking.Client;

/// <summary>
/// Calls the ParcelTracking API. The <see cref="HttpClient"/> is supplied by the host (normally through
/// <see cref="ServiceCollectionExtensions.AddParcelTrackingClient"/>), which also owns authentication and resilience.
/// </summary>
public sealed class ParcelTrackingClient
{
    private readonly HttpClient _httpClient;

    /// <summary>Creates a client over an HttpClient whose BaseAddress is the API root.</summary>
    /// <param name="httpClient">The configured HttpClient.</param>
    public ParcelTrackingClient(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        _httpClient = httpClient;
    }

    /// <summary>Reads a parcel, or <see langword="null"/> when the API does not know it.</summary>
    /// <param name="trackingNumber">The tracking number.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The parcel or null.</returns>
    public async Task<ParcelView?> GetParcelAsync(string trackingNumber, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trackingNumber);
        using var response = await _httpClient
            .GetAsync(new Uri($"api/parcels/{Uri.EscapeDataString(trackingNumber)}", UriKind.Relative), cancellationToken)
            .ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ParcelView>(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Registers a shipment.</summary>
    /// <param name="registration">The shipment.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The registered parcel.</returns>
    /// <exception cref="HttpRequestException">The API rejected the registration (409 when it already exists).</exception>
    public async Task<ParcelView> RegisterShipmentAsync(ShipmentRegistration registration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(registration);
        using var response = await _httpClient
            .PostAsJsonAsync(new Uri("api/shipments", UriKind.Relative), registration, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var parcel = await response.Content.ReadFromJsonAsync<ParcelView>(cancellationToken).ConfigureAwait(false);
        return parcel ?? throw new HttpRequestException("The API returned an empty registration response.");
    }
}
