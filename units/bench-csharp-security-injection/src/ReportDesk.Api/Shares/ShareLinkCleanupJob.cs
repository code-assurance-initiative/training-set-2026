namespace ReportDesk.Api.Shares;

/// <summary>
/// Share links live at most 30 days (<c>ValidDays</c>); this job deletes expired links, and the password hashes they
/// carry, every six hours.
/// </summary>
public sealed partial class ShareLinkCleanupJob(
    IServiceScopeFactory scopes,
    TimeProvider clock,
    ILogger<ShareLinkCleanupJob> logger) : BackgroundService
{
    public static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    public async Task<int> PurgeExpiredAsync(CancellationToken cancellationToken)
    {
        var scope = scopes.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            var store = scope.ServiceProvider.GetRequiredService<IShareStore>();
            var purged = await store.PurgeExpiredAsync(clock.GetUtcNow(), cancellationToken).ConfigureAwait(false);
            LogPurged(purged);
            return purged;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, clock);
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            await PurgeExpiredAsync(stoppingToken).ConfigureAwait(false);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Purged {Count} expired share links")]
    private partial void LogPurged(int count);
}
