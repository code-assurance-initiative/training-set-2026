using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ParcelTracking.Core.Carriers;
using ParcelTracking.Core.Notifications;
using ParcelTracking.Core.Tracking;
using ParcelTracking.UnitTests.TestSupport;

namespace ParcelTracking.UnitTests.Worker;

internal static class WorkerHostTestSupport
{
    public static ServiceProvider Services(FakeParcelStore store, FakeCarrierClient carrier, IMerchantNotifier notifier, TimeProvider clock) =>
        new ServiceCollection()
            .AddSingleton<IParcelStore>(store)
            .AddSingleton<ICarrierClient>(carrier)
            .AddSingleton(notifier)
            .AddSingleton(CarrierStatusMap.Default)
            .AddSingleton(clock)
            .AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
            .AddSingleton(typeof(ILogger<>), typeof(Logger<>))
            .AddScoped<TrackingService>()
            .BuildServiceProvider();

    public static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (!condition())
        {
            await Task.Delay(TimeSpan.FromMilliseconds(20), timeout.Token);
        }
    }
}

internal sealed class RecordingNotifier : IMerchantNotifier
{
    public List<PendingNotification> Sent { get; } = [];

    public bool Fail { get; init; }

    public Task NotifyAsync(PendingNotification notification, CancellationToken cancellationToken)
    {
        if (Fail)
        {
            throw new HttpRequestException("relay unavailable");
        }

        lock (Sent)
        {
            Sent.Add(notification);
        }

        return Task.CompletedTask;
    }
}
