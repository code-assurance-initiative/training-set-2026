using ParcelTracking.Core.Parcels;

namespace ParcelTracking.Core.Notifications;

/// <summary>
/// An outbox row: written in the same transaction as the status change it announces, delivered later by the worker.
/// </summary>
public sealed class PendingNotification
{
    public PendingNotification(Guid id, Guid parcelId, string merchantId, string trackingNumber, ParcelStatus status, DateTimeOffset occurredAt)
    {
        Id = id;
        ParcelId = parcelId;
        MerchantId = merchantId;
        TrackingNumber = trackingNumber;
        Status = status;
        OccurredAt = occurredAt;
        NextAttemptAt = occurredAt;
    }

    public Guid Id { get; private set; }

    public Guid ParcelId { get; private set; }

    public string MerchantId { get; private set; }

    public string TrackingNumber { get; private set; }

    public ParcelStatus Status { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public int Attempts { get; private set; }

    public DateTimeOffset NextAttemptAt { get; private set; }

    public DateTimeOffset? DeliveredAt { get; private set; }

    public void MarkDelivered(DateTimeOffset now) => DeliveredAt = now;

    public void MarkFailed(DateTimeOffset now)
    {
        Attempts++;
        var backoff = TimeSpan.FromMinutes(Math.Min(60, Math.Pow(2, Attempts)));
        NextAttemptAt = now + backoff;
    }
}
