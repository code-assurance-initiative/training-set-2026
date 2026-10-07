using System.Globalization;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace ParcelTracking.Worker;

/// <summary>
/// The worker serves no HTTP, so its probes are exec probes on a file: every health-check round that is not
/// Unhealthy rewrites the heartbeat, and the kubelet restarts the pod when the file goes stale.
/// </summary>
public sealed class HeartbeatPublisher(IOptions<PollingOptions> options, TimeProvider timeProvider) : IHealthCheckPublisher
{
    public async Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(report);
        if (report.Status == HealthStatus.Unhealthy)
        {
            return;
        }

        var stamp = timeProvider.GetUtcNow().ToString("O", CultureInfo.InvariantCulture);
        await File.WriteAllTextAsync(options.Value.HeartbeatPath, stamp, cancellationToken);
    }
}
