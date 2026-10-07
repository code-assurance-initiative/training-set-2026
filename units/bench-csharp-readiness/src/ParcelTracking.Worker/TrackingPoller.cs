using Microsoft.Extensions.Options;
using ParcelTracking.Core.Tracking;

namespace ParcelTracking.Worker;

/// <summary>
/// Asks the carrier API for new scans of every parcel that is still on its way, in batches of the parcels polled
/// least recently.
/// </summary>
public sealed class TrackingPoller(
    IServiceScopeFactory scopeFactory,
    IOptions<PollingOptions> options,
    TimeProvider timeProvider,
    ILogger<TrackingPoller> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = options.Value.Interval;
        while (true)
        {
            await PollOnceAsync();
            await Task.Delay(interval);
        }
    }

    private async Task PollOnceAsync()
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IParcelStore>();
        var tracking = scope.ServiceProvider.GetRequiredService<TrackingService>();
        var polling = options.Value;
        var started = timeProvider.GetTimestamp();

        var due = await store.ListDueForPollingAsync(timeProvider.GetUtcNow() - polling.PollAge, polling.BatchSize, CancellationToken.None);
        var changes = 0;
        foreach (var parcel in due)
        {
            try
            {
                changes += await tracking.RefreshAsync(parcel, CancellationToken.None);
            }
            catch (HttpRequestException ex)
            {
                logger.LogWarning(ex, "Polling parcel {TrackingNumber} at carrier {CarrierCode} failed", parcel.TrackingNumber, parcel.CarrierCode);
            }
        }

        logger.LogInformation(
            "Polled {ParcelCount} parcels in {ElapsedMs} ms; " +
            "{StatusChanges} status changes queued for merchants",
            due.Count,
            timeProvider.GetElapsedTime(started).TotalMilliseconds,
            changes);
    }
}
