using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using ParcelTracking.Worker;

namespace ParcelTracking.UnitTests.Worker;

public sealed class HeartbeatPublisherTests
{
    [Theory]
    [InlineData(HealthStatus.Healthy, true)]
    [InlineData(HealthStatus.Degraded, true)]
    [InlineData(HealthStatus.Unhealthy, false)]
    public async Task Writes_the_heartbeat_unless_unhealthy(HealthStatus status, bool written)
    {
        var path = Path.Combine(Path.GetTempPath(), $"heartbeat-{Guid.NewGuid():N}");
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.Zero));
        var publisher = new HeartbeatPublisher(Options.Create(new PollingOptions { HeartbeatPath = path }), clock);
        var report = new HealthReport(new Dictionary<string, HealthReportEntry>
        {
            ["any"] = new(status, null, TimeSpan.Zero, null, null),
        }, TimeSpan.Zero);

        try
        {
            await publisher.PublishAsync(report, TestContext.Current.CancellationToken);

            Assert.Equal(written, File.Exists(path));
            if (written)
            {
                Assert.Equal("2026-09-01T08:00:00.0000000+00:00", await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
            }
        }
        finally
        {
            File.Delete(path);
        }
    }
}
