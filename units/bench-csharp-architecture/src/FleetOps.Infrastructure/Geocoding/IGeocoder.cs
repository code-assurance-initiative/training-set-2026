namespace FleetOps.Infrastructure.Geocoding;

public interface IGeocoder
{
    Task<GeocodingResult> GeocodeAsync(Address address, CancellationToken cancellationToken);
}
