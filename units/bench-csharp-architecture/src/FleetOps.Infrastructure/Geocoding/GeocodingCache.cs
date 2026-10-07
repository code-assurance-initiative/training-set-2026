namespace FleetOps.Infrastructure.Geocoding;

/// <summary>Addresses already geocoded in this process. Depots and workshops are looked up over and over.</summary>
public sealed class GeocodingCache
{
    private const int Capacity = 10_000;
    private readonly Dictionary<string, GeoPoint> _entries = new(StringComparer.Ordinal);

    public bool TryGet(string key, out GeoPoint? point)
    {
        var found = _entries.TryGetValue(key, out var value);
        point = value;
        return found;
    }

    public void Put(string key, GeoPoint point)
    {
        if (_entries.Count >= Capacity)
        {
            _entries.Clear();
        }

        _entries[key] = point;
    }
}
