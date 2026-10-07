using System.Net;

namespace FleetOps.Api.IntegrationTests;

public sealed class HostTests(FleetApiFactory factory) : IClassFixture<FleetApiFactory>
{
    [Fact]
    public async Task HealthIsAnonymousAndHealthy()
    {
        var response = await factory.CreateClient().GetAsync("/health", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task EveryResponseCarriesTheSecurityHeaders()
    {
        var response = await factory.CreateClient().GetAsync("/health", TestContext.Current.CancellationToken);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Contains("default-src 'none'", response.Headers.GetValues("Content-Security-Policy").Single(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/vehicles")]
    [InlineData("/dashboard")]
    [InlineData("/maintenance/due")]
    public async Task FleetEndpointsRequireAToken(string path)
    {
        var response = await factory.CreateClient().GetAsync(path, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ATokenWithoutTheScopeIsForbidden()
    {
        var response = await factory.CreateClient("fleet.read").PostAsync("/vehicles", null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
