namespace ParcelTracking.Core.Parcels;

public sealed class TrackingEvent
{
    public TrackingEvent(Guid id, Guid parcelId, DateTimeOffset occurredAt, string carrierStatusCode, ParcelStatus status, string? location)
    {
        Id = id;
        ParcelId = parcelId;
        OccurredAt = occurredAt;
        CarrierStatusCode = carrierStatusCode;
        Status = status;
        Location = location;
    }

    public Guid Id { get; private set; }

    public Guid ParcelId { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public string CarrierStatusCode { get; private set; }

    public ParcelStatus Status { get; private set; }

    public string? Location { get; private set; }
}
