using FleetOps.Infrastructure.Scheduling;
using Microsoft.Extensions.Options;

namespace FleetOps.Worker;

/// <summary>Runs the maintenance-reminder job on the configured interval. The job itself lives in Infrastructure.</summary>
public sealed partial class MaintenanceReminderWorker(
    ReminderScheduler scheduler,
    IOptions<SchedulingOptions> options,
    TimeProvider clock,
    ILogger<MaintenanceReminderWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.RunEvery, clock);
        do
        {
            try
            {
                await scheduler.RunOnceAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                LogRunFailed(ex);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Reminder run failed; retrying on the next tick")]
    private partial void LogRunFailed(Exception exception);
}
