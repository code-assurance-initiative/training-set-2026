using ParcelTracking.Core.Notifications;
using ParcelTracking.Core.Parcels;

namespace ParcelTracking.UnitTests.Tracking;

public sealed class PendingNotificationTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 1, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_new_notification_is_due_immediately() =>
        Assert.Equal(Now, New().NextAttemptAt);

    [Fact]
    public void Failures_back_off_exponentially_up_to_an_hour()
    {
        var notification = New();

        notification.MarkFailed(Now);
        Assert.Equal(Now.AddMinutes(2), notification.NextAttemptAt);
        notification.MarkFailed(Now);
        Assert.Equal(Now.AddMinutes(4), notification.NextAttemptAt);
        for (var i = 0; i < 10; i++)
        {
            notification.MarkFailed(Now);
        }

        Assert.Equal(Now.AddHours(1), notification.NextAttemptAt);
        Assert.Equal(12, notification.Attempts);
    }

    [Fact]
    public void Delivery_is_stamped()
    {
        var notification = New();
        notification.MarkDelivered(Now.AddSeconds(3));
        Assert.Equal(Now.AddSeconds(3), notification.DeliveredAt);
    }

    private static PendingNotification New() =>
        new(Guid.NewGuid(), Guid.NewGuid(), "merchant-1", "NP100200300", ParcelStatus.InTransit, Now);
}
