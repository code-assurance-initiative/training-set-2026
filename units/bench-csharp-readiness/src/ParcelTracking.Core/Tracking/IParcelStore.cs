using ParcelTracking.Core.Notifications;
using ParcelTracking.Core.Parcels;

namespace ParcelTracking.Core.Tracking;

public interface IParcelStore
{
    Task<Parcel?> FindByTrackingNumberAsync(string trackingNumber, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(string trackingNumber, CancellationToken cancellationToken);

    Task<IReadOnlyList<Parcel>> ListDueForPollingAsync(DateTimeOffset polledBefore, int max, CancellationToken cancellationToken);

    Task<IReadOnlyList<PendingNotification>> ListDueNotificationsAsync(DateTimeOffset now, int max, CancellationToken cancellationToken);

    Task<DeliveryPerformance> GetDeliveryPerformanceAsync(string merchantId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes delivered notifications older than <paramref name="notificationsBefore"/> and delivered or returned
    /// parcels (with their history) last updated before <paramref name="parcelsBefore"/>. Returns the rows deleted.
    /// </summary>
    Task<int> PurgeAsync(DateTimeOffset notificationsBefore, DateTimeOffset parcelsBefore, CancellationToken cancellationToken);

    void Add(Parcel parcel);

    void Add(PendingNotification notification);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
