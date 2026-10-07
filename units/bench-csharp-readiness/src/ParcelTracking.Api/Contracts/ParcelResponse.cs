using ParcelTracking.Core.Carriers;
using ParcelTracking.Core.Parcels;

namespace ParcelTracking.Api.Contracts;

public sealed record ParcelResponse(
    string TrackingNumber,
    string CarrierCode,
    string? CarrierName,
    string Status,
    DateTimeOffset RegisteredAt,
    DateTimeOffset? DeliveredAt,
    string? PickupPointId,
    IReadOnlyList<TrackingEventResponse> Events)
{
    public static ParcelResponse From(Parcel parcel, CarrierInfo? carrier)
    {
        ArgumentNullException.ThrowIfNull(parcel);
        return new ParcelResponse(
            parcel.TrackingNumber,
            parcel.CarrierCode,
            carrier?.DisplayName,
            parcel.Status.ToString(),
            parcel.RegisteredAt,
            parcel.DeliveredAt,
            parcel.PickupPointId,
            parcel.Events
                .OrderBy(e => e.OccurredAt)
                .Select(e => new TrackingEventResponse(e.OccurredAt, e.Status.ToString(), e.Location))
                .ToList());
    }
}

public sealed record TrackingEventResponse(DateTimeOffset OccurredAt, string Status, string? Location);
