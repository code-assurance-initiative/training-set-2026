using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quellbrook.Notifier.Persistence;

namespace Quellbrook.Notifier.Retention;

public sealed record PurgeResult(int Recipients, int NotificationLog, int ProcessedMessages);

/// <summary>Deletes what has outlived its retention period, every hour (docs/privacy.md).</summary>
public sealed partial class RetentionSweeper(
    IServiceScopeFactory scopes,
    TimeProvider time,
    IOptions<RetentionOptions> options,
    ILogger<RetentionSweeper> logger) : BackgroundService
{
    public async Task<PurgeResult> PurgeAsync(CancellationToken cancellationToken)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotifierDbContext>();
        var now = time.GetUtcNow();
        var settings = options.Value;
        var deliveredBefore = now - settings.RecipientAfterDelivery;
        var announcedBefore = now - settings.RecipientWithoutDelivery;
        var sentBefore = now - settings.NotificationLog;
        var processedBefore = now - settings.ProcessedMessages;

        var recipients = await db.Recipients
            .Where(recipient => recipient.CompletedAt < deliveredBefore || (recipient.CompletedAt == null && recipient.ReceivedAt < announcedBefore))
            .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        var log = await db.NotificationLog.Where(entry => entry.SentAt < sentBefore).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        var processed = await db.ProcessedMessages.Where(message => message.ProcessedAt < processedBefore).ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        var result = new PurgeResult(recipients, log, processed);
        LogPurged(result.Recipients, result.NotificationLog, result.ProcessedMessages);
        return result;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.Interval, time);
        do
        {
            try
            {
                await PurgeAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (DbException exception)
            {
                LogFailed(exception);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Retention: deleted {Recipients} recipients, {LogEntries} notification log entries, {Messages} processed-message ids")]
    private partial void LogPurged(int recipients, int logEntries, int messages);

    [LoggerMessage(Level = LogLevel.Error, Message = "Retention sweep failed; retrying at the next interval")]
    private partial void LogFailed(Exception exception);
}
