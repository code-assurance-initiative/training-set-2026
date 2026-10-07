using Microsoft.Extensions.Logging;
using ParcelTracking.Core.Carriers;
using ParcelTracking.Core.Notifications;
using ParcelTracking.Core.Parcels;

namespace ParcelTracking.Core.Tracking;

public sealed partial class TrackingService(
    IParcelStore store,
    ICarrierClient carrier,
    CarrierStatusMap statusMap,
    TimeProvider timeProvider,
    ILogger<TrackingService> logger)
{
    public async Task<Parcel?> RegisterAsync(string trackingNumber, string merchantId, string carrierCode, string destinationPostalCode, CancellationToken cancellationToken)
    {
        if (await store.ExistsAsync(trackingNumber, cancellationToken))
        {
            return null;
        }

        var parcel = Parcel.Register(trackingNumber, merchantId, carrierCode, destinationPostalCode, timeProvider.GetUtcNow());
        store.Add(parcel);
        await store.SaveChangesAsync(cancellationToken);
        LogRegistered(parcel.TrackingNumber, parcel.CarrierCode, parcel.MerchantId);
        return parcel;
    }

    public Task<Parcel?> FindAsync(string trackingNumber, CancellationToken cancellationToken) =>
        store.FindByTrackingNumberAsync(trackingNumber, cancellationToken);

    public async Task<RedirectOutcome> RedirectAsync(string trackingNumber, string pickupPointId, DateOnly? holdUntil, string? note, CancellationToken cancellationToken)
    {
        var parcel = await store.FindByTrackingNumberAsync(trackingNumber, cancellationToken);
        if (parcel is null)
        {
            return RedirectOutcome.NotFound;
        }

        if (StatusTransitions.IsTerminal(parcel.Status))
        {
            return RedirectOutcome.AlreadyCompleted;
        }

        parcel.RedirectToPickupPoint(pickupPointId, holdUntil, note);
        await store.SaveChangesAsync(cancellationToken);
        return RedirectOutcome.Redirected;
    }

    /// <summary>
    /// Pulls the carrier's scans since the last poll, records them and queues one merchant notification per status
    /// change. The status change and its notification are saved in one transaction (outbox).
    /// </summary>
    public async Task<int> RefreshAsync(Parcel parcel, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parcel);
        var scans = await carrier.GetScansAsync(parcel.CarrierCode, parcel.TrackingNumber, parcel.LastPolledAt, cancellationToken);
        var changes = 0;
        foreach (var scan in scans.OrderBy(s => s.OccurredAt))
        {
            if (!statusMap.TryNormalise(parcel.CarrierCode, scan.StatusCode, out var status))
            {
                LogUnmappedStatus(parcel.CarrierCode, scan.StatusCode);
                continue;
            }

            var trackingEvent = new TrackingEvent(Guid.CreateVersion7(scan.OccurredAt), parcel.Id, scan.OccurredAt, scan.StatusCode, status, scan.Location);
            if (parcel.Record(trackingEvent))
            {
                store.Add(new PendingNotification(Guid.CreateVersion7(), parcel.Id, parcel.MerchantId, parcel.TrackingNumber, status, scan.OccurredAt));
                changes++;
            }
        }

        parcel.MarkPolled(timeProvider.GetUtcNow());
        await store.SaveChangesAsync(cancellationToken);
        return changes;
    }

    public Task<DeliveryPerformance> GetDeliveryPerformanceAsync(string merchantId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken) =>
        store.GetDeliveryPerformanceAsync(merchantId, from, to, cancellationToken);

    [LoggerMessage(Level = LogLevel.Information, Message = "Registered parcel {TrackingNumber} with carrier {CarrierCode} for merchant {MerchantId}")]
    private partial void LogRegistered(string trackingNumber, string carrierCode, string merchantId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Carrier {CarrierCode} reported unmapped status code {StatusCode}; scan ignored")]
    private partial void LogUnmappedStatus(string carrierCode, string statusCode);
}
