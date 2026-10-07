namespace ParcelTracking.Core.Parcels;

public sealed class Parcel
{
    private readonly List<TrackingEvent> _events = [];

    private Parcel(Guid id, string trackingNumber, string merchantId, string carrierCode, string destinationPostalCode, DateTimeOffset registeredAt)
    {
        Id = id;
        TrackingNumber = trackingNumber;
        MerchantId = merchantId;
        CarrierCode = carrierCode;
        DestinationPostalCode = destinationPostalCode;
        RegisteredAt = registeredAt;
        LastPolledAt = registeredAt;
        Status = ParcelStatus.Registered;
    }

    public Guid Id { get; private set; }

    public string TrackingNumber { get; private set; }

    public string MerchantId { get; private set; }

    public string CarrierCode { get; private set; }

    public string DestinationPostalCode { get; private set; }

    public ParcelStatus Status { get; private set; }

    public DateTimeOffset RegisteredAt { get; private set; }

    public DateTimeOffset LastPolledAt { get; private set; }

    public DateTimeOffset? DeliveredAt { get; private set; }

    public string? PickupPointId { get; private set; }

    public DateOnly? HoldUntil { get; private set; }

    public string? RedirectNote { get; private set; }

    public IReadOnlyList<TrackingEvent> Events => _events;

    public static Parcel Register(string trackingNumber, string merchantId, string carrierCode, string destinationPostalCode, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trackingNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(merchantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(carrierCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPostalCode);
        return new Parcel(Guid.CreateVersion7(now), trackingNumber, merchantId, carrierCode, destinationPostalCode, now);
    }

    /// <summary>Records a carrier scan and returns whether it changed the parcel's status.</summary>
    public bool Record(TrackingEvent trackingEvent)
    {
        ArgumentNullException.ThrowIfNull(trackingEvent);
        if (_events.Exists(e => e.CarrierStatusCode == trackingEvent.CarrierStatusCode && e.OccurredAt == trackingEvent.OccurredAt))
        {
            return false;
        }

        _events.Add(trackingEvent);
        if (!StatusTransitions.CanMove(Status, trackingEvent.Status))
        {
            return false;
        }

        Status = trackingEvent.Status;
        if (Status == ParcelStatus.Delivered)
        {
            DeliveredAt = trackingEvent.OccurredAt;
        }

        return true;
    }

    public void MarkPolled(DateTimeOffset now) => LastPolledAt = now;

    public void RedirectToPickupPoint(string pickupPointId, DateOnly? holdUntil, string? note)
    {
        if (StatusTransitions.IsTerminal(Status))
        {
            throw new InvalidOperationException($"Parcel {TrackingNumber} is {Status} and can no longer be redirected.");
        }

        PickupPointId = pickupPointId;
        HoldUntil = holdUntil;
        RedirectNote = note;
    }
}
