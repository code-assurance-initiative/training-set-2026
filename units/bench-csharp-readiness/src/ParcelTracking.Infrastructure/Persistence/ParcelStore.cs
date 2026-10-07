using Microsoft.EntityFrameworkCore;
using ParcelTracking.Core.Notifications;
using ParcelTracking.Core.Parcels;
using ParcelTracking.Core.Tracking;

namespace ParcelTracking.Infrastructure.Persistence;

public sealed class ParcelStore(TrackingDbContext db) : IParcelStore
{
    public Task<bool> ExistsAsync(string trackingNumber, CancellationToken cancellationToken) =>
        db.Parcels.AnyAsync(p => p.TrackingNumber == trackingNumber, cancellationToken);

    public async Task<Parcel?> FindByTrackingNumberAsync(string trackingNumber, CancellationToken cancellationToken)
    {
        return await db.Parcels
            .Include(p => p.Events)
            .SingleOrDefaultAsync(p => p.TrackingNumber == trackingNumber);
    }

    public async Task<IReadOnlyList<Parcel>> ListDueForPollingAsync(DateTimeOffset polledBefore, int max, CancellationToken cancellationToken)
    {
        return await db.Parcels
            .Include(p => p.Events)
            .Where(p => p.Status != ParcelStatus.Delivered && p.Status != ParcelStatus.Returned && p.LastPolledAt < polledBefore)
            .OrderBy(p => p.LastPolledAt)
            .Take(max)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PendingNotification>> ListDueNotificationsAsync(DateTimeOffset now, int max, CancellationToken cancellationToken)
    {
        return await db.PendingNotifications
            .Where(n => n.DeliveredAt == null && n.NextAttemptAt <= now)
            .OrderBy(n => n.NextAttemptAt)
            .Take(max)
            .ToListAsync(cancellationToken);
    }

    public async Task<DeliveryPerformance> GetDeliveryPerformanceAsync(string merchantId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
    {
        var parcels = await db.Parcels
            .AsNoTracking()
            .Where(p => p.MerchantId == merchantId && p.RegisteredAt >= from && p.RegisteredAt < to)
            .Select(p => new { p.Status, p.RegisteredAt, p.DeliveredAt })
            .ToListAsync(cancellationToken);

        var daysToDeliver = parcels
            .Where(p => p.DeliveredAt is not null)
            .Select(p => (p.DeliveredAt.GetValueOrDefault() - p.RegisteredAt).TotalDays)
            .Order()
            .ToList();

        return new DeliveryPerformance(
            merchantId,
            parcels.Count,
            parcels.Count(p => p.Status == ParcelStatus.Delivered),
            parcels.Count(p => p.Status == ParcelStatus.DeliveryFailed),
            parcels.Count(p => p.Status == ParcelStatus.Returned),
            daysToDeliver.Count == 0 ? null : daysToDeliver[daysToDeliver.Count / 2]);
    }

    public async Task<int> PurgeAsync(DateTimeOffset notificationsBefore, DateTimeOffset parcelsBefore, CancellationToken cancellationToken)
    {
        var notifications = await db.PendingNotifications
            .Where(n => n.DeliveredAt != null && n.DeliveredAt < notificationsBefore)
            .ExecuteDeleteAsync(cancellationToken);
        var events = await db.TrackingEvents
            .Where(e => db.Parcels.Any(p => p.Id == e.ParcelId
                && (p.Status == ParcelStatus.Delivered || p.Status == ParcelStatus.Returned)
                && p.LastPolledAt < parcelsBefore))
            .ExecuteDeleteAsync(cancellationToken);
        var parcels = await db.Parcels
            .Where(p => (p.Status == ParcelStatus.Delivered || p.Status == ParcelStatus.Returned) && p.LastPolledAt < parcelsBefore)
            .ExecuteDeleteAsync(cancellationToken);
        return notifications + events + parcels;
    }

    public void Add(Parcel parcel) => db.Parcels.Add(parcel);

    public void Add(PendingNotification notification) => db.PendingNotifications.Add(notification);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
