using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Quellbrook.Notifier.Hosting;

public sealed class HeartbeatOptions
{
    public const string SectionName = "Heartbeat";

    public string Path { get; set; } = "/tmp/heartbeat";

    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(30);
}

/// <summary>
/// The worker serves no HTTP, so the kubelet's probes read a file instead: it is rewritten after every health-check
/// round that is not Unhealthy, and the probes fail when it grows old (deploy/k8s/deployment.yaml).
/// </summary>
public sealed partial class HeartbeatPublisher(
    HealthCheckService health,
    TimeProvider time,
    IOptions<HeartbeatOptions> options,
    ILogger<HeartbeatPublisher> logger) : BackgroundService
{
    public async Task<HealthStatus> BeatAsync(CancellationToken cancellationToken)
    {
        var report = await health.CheckHealthAsync(cancellationToken).ConfigureAwait(false);
        if (report.Status == HealthStatus.Unhealthy)
        {
            LogUnhealthy(string.Join(", ", report.Entries.Where(entry => entry.Value.Status == HealthStatus.Unhealthy).Select(entry => entry.Key)));
            return report.Status;
        }

        await File.WriteAllTextAsync(options.Value.Path, time.GetUtcNow().ToString("O"), cancellationToken).ConfigureAwait(false);
        return report.Status;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.Interval, time);
        do
        {
            await BeatAsync(stoppingToken).ConfigureAwait(false);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Unhealthy: {Checks}; heartbeat not written")]
    private partial void LogUnhealthy(string checks);
}
