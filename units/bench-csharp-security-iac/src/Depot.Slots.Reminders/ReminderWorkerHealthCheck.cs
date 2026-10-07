using Depot.Slots.Core.Reminders;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Depot.Slots.Reminders;

/// <summary>Ready once a pass has completed recently; a stalled loop turns the pod unready.</summary>
public sealed class ReminderWorkerHealthCheck(
    IEnumerable<IHostedService> hostedServices,
    IOptions<ReminderOptions> options,
    TimeProvider clock) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var worker = hostedServices.OfType<ReminderWorker>().Single();
        if (worker.LastCompletedPass is not { } last)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("No reminder pass has completed yet."));
        }

        var stale = clock.GetUtcNow() - last > options.Value.PollInterval * 3;
        return Task.FromResult(stale
            ? HealthCheckResult.Unhealthy($"Last reminder pass finished at {last:O}.")
            : HealthCheckResult.Healthy());
    }
}
