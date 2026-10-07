namespace FleetOps.Infrastructure.Geocoding;

public sealed record GeoPoint(double Latitude, double Longitude);

public sealed record Address(string Street, string PostalCode, string City, string CountryCode)
{
    public string Normalised => $"{Street}, {PostalCode} {City}, {CountryCode}".ToUpperInvariant();
}

public sealed record GeocodingResult(Address Address, GeoPoint? Point, double Confidence);

public sealed record GeocodingResponse(IReadOnlyList<GeocodingCandidate> Candidates);

public sealed record GeocodingCandidate(double Lat, double Lon, double Score);

public sealed class GeocodingException : Exception
{
    public GeocodingException()
    {
    }

    public GeocodingException(string message)
        : base(message)
    {
    }

    public GeocodingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
