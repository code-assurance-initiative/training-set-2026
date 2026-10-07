using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ParcelTracking.Core.Notifications;
using ParcelTracking.Core.Parcels;
using ParcelTracking.Infrastructure.Persistence;

namespace ParcelTracking.IntegrationTests;

public sealed class ParcelStoreTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 9, 1, 8, 0, 0, TimeSpan.Zero);

    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly TrackingDbContext _db;

    public ParcelStoreTests()
    {
        _connection.Open();
        _db = new TrackingDbContext(new DbContextOptionsBuilder<TrackingDbContext>()
            .UseSqlite(_connection)
            .ReplaceService<IModelCustomizer, SqliteModelCustomizer>()
            .Options);
        _db.Database.EnsureCreated();
    }

    [Fact]
    public async Task Purge_deletes_only_completed_parcels_and_delivered_notifications_past_retention()
    {
        var store = new ParcelStore(_db);
        var delivered = Parcel.Register("NP000000001", "m", "NORDPOST", "0150", Now.AddDays(-400));
        delivered.Record(new TrackingEvent(Guid.NewGuid(), delivered.Id, Now.AddDays(-399), "DLV", ParcelStatus.Delivered, null));
        var inTransit = Parcel.Register("NP000000002", "m", "NORDPOST", "0150", Now.AddDays(-400));
        var notification = new PendingNotification(Guid.NewGuid(), delivered.Id, "m", "NP000000001", ParcelStatus.Delivered, Now.AddDays(-399));
        notification.MarkDelivered(Now.AddDays(-399));
        store.Add(delivered);
        store.Add(inTransit);
        store.Add(notification);
        await store.SaveChangesAsync(TestContext.Current.CancellationToken);

        var deleted = await store.PurgeAsync(Now.AddDays(-30), Now.AddDays(-180), TestContext.Current.CancellationToken);

        Assert.Equal(3, deleted);
        Assert.Equal(["NP000000002"], await _db.Parcels.Select(p => p.TrackingNumber).ToListAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await _db.TrackingEvents.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await _db.PendingNotifications.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Polling_picks_the_least_recently_polled_active_parcels()
    {
        var store = new ParcelStore(_db);
        var older = Parcel.Register("NP000000011", "m", "NORDPOST", "0150", Now.AddHours(-3));
        var newer = Parcel.Register("NP000000012", "m", "NORDPOST", "0150", Now.AddHours(-2));
        var fresh = Parcel.Register("NP000000013", "m", "NORDPOST", "0150", Now);
        store.Add(newer);
        store.Add(older);
        store.Add(fresh);
        await store.SaveChangesAsync(TestContext.Current.CancellationToken);

        var due = await store.ListDueForPollingAsync(Now.AddHours(-1), 10, TestContext.Current.CancellationToken);

        Assert.Equal(["NP000000011", "NP000000012"], due.Select(p => p.TrackingNumber));
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
