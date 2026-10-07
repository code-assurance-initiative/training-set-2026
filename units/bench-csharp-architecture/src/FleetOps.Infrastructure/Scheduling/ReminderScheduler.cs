using FleetOps.Application.Features.Maintenance;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FleetOps.Infrastructure.Scheduling;

/// <summary>
/// One reminder run, in a fresh scope: asks the application what is due, dispatches it, and logs when the reminders
/// held back for quiet hours are planned to go out.
/// </summary>
public sealed partial class ReminderScheduler(
    IServiceScopeFactory scopes,
    MaintenanceReminderPlanner planner,
    QuietHours quietHours,
    TimeProvider clock,
    ILogger<ReminderScheduler> logger)
{
    public async Task<ReminderRun> RunOnceAsync(CancellationToken cancellationToken)
    {
        var scope = scopes.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            var dispatcher = scope.ServiceProvider.GetRequiredService<ReminderDispatcher>();
            var due = await sender.Send(new GetDueMaintenanceQuery(), cancellationToken).ConfigureAwait(false);
            var run = await dispatcher.DispatchAsync(due, cancellationToken).ConfigureAwait(false);
            LogRun(run.Due, run.Sent, run.Deferred);
            if (run.Deferred > 0 && planner.Plan(due, clock.GetLocalNow(), quietHours) is [var first, ..])
            {
                LogDeferred(run.Deferred, first.SendAt);
            }

            return run;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Reminder run: {Due} due, {Sent} sent, {Deferred} deferred")]
    private partial void LogRun(int due, int sent, int deferred);

    [LoggerMessage(Level = LogLevel.Information, Message = "{Deferred} reminder(s) deferred; next send window opens {SendAt}")]
    private partial void LogDeferred(int deferred, DateTimeOffset sendAt);
}
