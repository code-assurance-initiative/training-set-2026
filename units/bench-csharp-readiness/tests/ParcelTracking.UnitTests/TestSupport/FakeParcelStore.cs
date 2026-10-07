using ParcelTracking.Core.Notifications;
using ParcelTracking.Core.Parcels;
using ParcelTracking.Core.Tracking;

namespace ParcelTracking.UnitTests.TestSupport;

public sealed class FakeParcelStore : IParcelStore
{
    public List<Parcel> Parcels { get; } = [];

    public List<PendingNotification> Notifications { get; } = [];

    public int Saves { get; private set; }

    public Task<Parcel?> FindByTrackingNumberAsync(string trackingNumber, CancellationToken cancellationToken) =>
        Task.FromResult(Parcels.Find(p => p.TrackingNumber == trackingNumber));

    public Task<bool> ExistsAsync(string trackingNumber, CancellationToken cancellationToken) =>
        Task.FromResult(Parcels.Exists(p => p.TrackingNumber == trackingNumber));

    public Task<IReadOnlyList<Parcel>> ListDueForPollingAsync(DateTimeOffset polledBefore, int max, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Parcel>>(Parcels.Where(p => p.LastPolledAt < polledBefore).Take(max).ToList());

    public Task<IReadOnlyList<PendingNotification>> ListDueNotificationsAsync(DateTimeOffset now, int max, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<PendingNotification>>(Notifications.Where(n => n.DeliveredAt is null && n.NextAttemptAt <= now).Take(max).ToList());

    public Task<DeliveryPerformance> GetDeliveryPerformanceAsync(string merchantId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken) =>
        Task.FromResult(new DeliveryPerformance(merchantId, Parcels.Count(p => p.MerchantId == merchantId), 0, 0, 0, null));

    public Task<int> PurgeAsync(DateTimeOffset notificationsBefore, DateTimeOffset parcelsBefore, CancellationToken cancellationToken) =>
        Task.FromResult(Notifications.RemoveAll(n => n.DeliveredAt < notificationsBefore)
            + Parcels.RemoveAll(p => StatusTransitions.IsTerminal(p.Status) && p.LastPolledAt < parcelsBefore));

    public void Add(Parcel parcel) => Parcels.Add(parcel);

    public void Add(PendingNotification notification) => Notifications.Add(notification);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        Saves++;
        return Task.CompletedTask;
    }
}
