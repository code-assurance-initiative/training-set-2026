using Microsoft.Extensions.Options;
using ParcelTracking.Core.Tracking;

namespace ParcelTracking.Worker;

/// <summary>Deletes data that has outlived its retention period (docs/operations/data-retention.md).</summary>
public sealed partial class RetentionSweeper(
    IServiceScopeFactory scopeFactory,
    IOptions<RetentionOptions> options,
    TimeProvider timeProvider,
    ILogger<RetentionSweeper> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.SweepInterval, timeProvider);
        try
        {
            do
            {
                await SweepAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    public async Task<int> SweepAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IParcelStore>();
        var now = timeProvider.GetUtcNow();
        var retention = options.Value;
        var deleted = await store.PurgeAsync(now - retention.DeliveredNotifications, now - retention.CompletedParcels, cancellationToken);
        LogSwept(deleted);
        return deleted;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Retention sweep deleted {RowCount} rows")]
    private partial void LogSwept(int rowCount);
}
