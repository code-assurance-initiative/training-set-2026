using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Quellbrook.Notifier.Hosting;
using Quellbrook.Notifier.Persistence;

namespace Quellbrook.Notifier.UnitTests.Hosting;

public sealed class HostingTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"heartbeat-{Guid.NewGuid():N}");

    [Theory]
    [InlineData(HealthStatus.Healthy, true)]
    [InlineData(HealthStatus.Degraded, true)]
    [InlineData(HealthStatus.Unhealthy, false)]
    public async Task TheHeartbeatIsWrittenUnlessTheWorkerIsUnhealthy(HealthStatus status, bool written)
    {
        var health = Substitute.For<HealthCheckService>();
        health.CheckHealthAsync(Arg.Any<Func<HealthCheckRegistration, bool>?>(), Arg.Any<CancellationToken>())
            .Returns(new HealthReport(new Dictionary<string, HealthReportEntry>
            {
                ["database"] = new(status, null, TimeSpan.Zero, null, null),
            }, TimeSpan.Zero));
        var publisher = new HeartbeatPublisher(health, new FakeTimeProvider(), Options.Create(new HeartbeatOptions { Path = _path }),
            NullLogger<HeartbeatPublisher>.Instance);

        var reported = await publisher.BeatAsync(TestContext.Current.CancellationToken);

        Assert.Equal(status, reported);
        Assert.Equal(written, File.Exists(_path));
    }

    [Fact]
    public void TheDesignTimeContextTargetsPostgreSql()
    {
        using var context = new DesignTimeDbContextFactory().CreateDbContext([]);

        Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", context.Database.ProviderName);
    }

    public void Dispose() => File.Delete(_path);
}
