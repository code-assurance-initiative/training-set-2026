using System.Collections.Frozen;
using ParcelTracking.Core.Parcels;

namespace ParcelTracking.Core.Carriers;

/// <summary>
/// Translates carrier-specific scan codes into the service's own <see cref="ParcelStatus"/>. Codes are compared after
/// trimming and upper-casing, because carriers are not consistent about either.
/// </summary>
public sealed class CarrierStatusMap
{
    private readonly FrozenDictionary<string, ParcelStatus> _statuses;

    public CarrierStatusMap(IEnumerable<KeyValuePair<string, ParcelStatus>> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        _statuses = entries.ToFrozenDictionary(e => Key(e.Key), e => e.Value, StringComparer.Ordinal);
    }

    public static CarrierStatusMap Default { get; } = new(
    [
        new("NORDPOST:REG", ParcelStatus.Registered),
        new("NORDPOST:ACC", ParcelStatus.InTransit),
        new("NORDPOST:HUB", ParcelStatus.InTransit),
        new("NORDPOST:OFD", ParcelStatus.OutForDelivery),
        new("NORDPOST:PUP", ParcelStatus.HeldAtPickupPoint),
        new("NORDPOST:DLV", ParcelStatus.Delivered),
        new("NORDPOST:NDL", ParcelStatus.DeliveryFailed),
        new("NORDPOST:RTS", ParcelStatus.Returned),
        new("SWIFTLINE:CREATED", ParcelStatus.Registered),
        new("SWIFTLINE:IN_TRANSIT", ParcelStatus.InTransit),
        new("SWIFTLINE:WITH_COURIER", ParcelStatus.OutForDelivery),
        new("SWIFTLINE:AT_PARCELSHOP", ParcelStatus.HeldAtPickupPoint),
        new("SWIFTLINE:DELIVERED", ParcelStatus.Delivered),
        new("SWIFTLINE:ATTEMPT_FAILED", ParcelStatus.DeliveryFailed),
        new("SWIFTLINE:RETURNED", ParcelStatus.Returned),
    ]);

    public int Count => _statuses.Count;

    public bool TryNormalise(string carrierCode, string statusCode, out ParcelStatus status)
    {
        ArgumentNullException.ThrowIfNull(carrierCode);
        ArgumentNullException.ThrowIfNull(statusCode);
        return _statuses.TryGetValue(Key(carrierCode) + ":" + Key(statusCode), out status);
    }

    private static string Key(string value) => value.Trim().ToUpperInvariant();
}
