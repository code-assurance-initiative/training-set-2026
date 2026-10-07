using System.Net;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ParcelTracking.Infrastructure.Health;
using ParcelTracking.UnitTests.TestSupport;

namespace ParcelTracking.UnitTests.Infrastructure;

public sealed class CarrierApiHealthCheckTests
{
    [Fact]
    public async Task A_reachable_carrier_api_is_healthy()
    {
        var check = new CarrierApiHealthCheck(new SingleClientFactory(StubHttpHandler.Returning(HttpStatusCode.OK)));

        var result = await check.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task An_error_status_is_degraded()
    {
        var check = new CarrierApiHealthCheck(new SingleClientFactory(StubHttpHandler.Returning(HttpStatusCode.ServiceUnavailable)));

        var result = await check.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Contains("503", result.Description, StringComparison.Ordinal);
    }

    private sealed class SingleClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false) { BaseAddress = new Uri("https://carrier.test/") };
    }
}
