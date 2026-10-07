using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace FleetOps.Infrastructure.Geocoding;

/// <summary>Geocodes depot and roadside addresses; answers repeat lookups from <see cref="GeocodingCache"/>.</summary>
public sealed class HttpGeocoder(HttpClient http, GeocodingCache cache, IOptions<GeocodingOptions> options) : IGeocoder
{
    public async Task<GeocodingResult> GeocodeAsync(Address address, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(address);
        if (cache.TryGet(address.Normalised, out var cached))
        {
            return new GeocodingResult(address, cached, 1.0);
        }

        var response = await http
            .GetFromJsonAsync<GeocodingResponse>(new Uri($"search?q={Uri.EscapeDataString(address.Normalised)}", UriKind.Relative), cancellationToken)
            .ConfigureAwait(false) ?? throw new GeocodingException("The geocoder returned no body.");
        var best = response.Candidates.OrderByDescending(c => c.Score).FirstOrDefault();
        if (best is null || best.Score < options.Value.MinimumConfidence)
        {
            return new GeocodingResult(address, null, best?.Score ?? 0);
        }

        var point = new GeoPoint(best.Lat, best.Lon);
        cache.Put(address.Normalised, point);
        return new GeocodingResult(address, point, best.Score);
    }
}
