using FleetOps.Infrastructure.Telematics;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FleetOps.Diagnostics;

/// <summary>Degraded, not unhealthy, when the vendor is down: due-maintenance falls back to stored odometers.</summary>
public sealed class TelematicsHealthCheck(ITelematicsClient telematics) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            return await telematics.PingAsync(cancellationToken).ConfigureAwait(false)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Degraded("The telematics API answered with an error.");
        }
        catch (HttpRequestException ex)
        {
            return HealthCheckResult.Degraded("The telematics API is unreachable.", ex);
        }
    }
}
