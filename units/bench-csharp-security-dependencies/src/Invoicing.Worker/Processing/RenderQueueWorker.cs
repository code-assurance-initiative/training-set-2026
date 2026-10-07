using Cronos;
using Microsoft.Extensions.Options;

namespace Invoicing.Worker.Processing;

/// <summary>Wakes on the configured cron schedule and drains one batch of the render queue.</summary>
public sealed partial class RenderQueueWorker(
    RenderQueueProcessor processor,
    IOptions<WorkerOptions> options,
    TimeProvider clock,
    ILogger<RenderQueueWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var schedule = CronExpression.Parse(options.Value.Schedule);
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = clock.GetUtcNow();
            var next = schedule.GetNextOccurrence(now, TimeZoneInfo.Utc);
            if (next is null)
            {
                LogScheduleExhausted(options.Value.Schedule);
                return;
            }

            await Task.Delay(next.Value - now, clock, stoppingToken).ConfigureAwait(false);
            var result = await processor.ProcessBatchAsync(options.Value.BatchSize, stoppingToken).ConfigureAwait(false);
            LogBatch(result.Claimed, result.Sent);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Render batch: {Claimed} claimed, {Sent} sent")]
    private partial void LogBatch(int claimed, int sent);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Schedule '{Schedule}' has no further occurrence; the worker stops")]
    private partial void LogScheduleExhausted(string schedule);
}
