using FleetOps.Domain.Maintenance;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FleetOps.Diagnostics;

/// <summary>Unhealthy if the maintenance plan this build ships with has no intervals (a broken release).</summary>
public sealed class MaintenancePlanHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(MaintenancePlan.Standard.Intervals.Count > 0
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("The maintenance plan has no service intervals."));
}
