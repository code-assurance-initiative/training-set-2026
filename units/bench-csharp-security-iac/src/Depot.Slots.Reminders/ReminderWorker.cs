using Depot.Slots.Core.Reminders;
using Microsoft.Extensions.Options;

namespace Depot.Slots.Reminders;

/// <summary>Runs a reminder pass every poll interval until the host stops.</summary>
public sealed partial class ReminderWorker(
    ReminderDispatcher dispatcher,
    IOptions<ReminderOptions> options,
    TimeProvider clock,
    ILogger<ReminderWorker> logger) : BackgroundService
{
    /// <summary>When the last pass finished, for the health check; null until the first one has.</summary>
    public DateTimeOffset? LastCompletedPass { get; private set; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.PollInterval, clock);
        do
        {
            var sent = await dispatcher.DispatchDueAsync(stoppingToken).ConfigureAwait(false);
            LastCompletedPass = clock.GetUtcNow();
            if (sent > 0)
            {
                LogSent(sent);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Sent {Count} dock reminders")]
    private partial void LogSent(int count);
}
