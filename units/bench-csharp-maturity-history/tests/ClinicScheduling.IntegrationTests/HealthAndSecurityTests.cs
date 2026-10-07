using System.Net;

namespace ClinicScheduling.IntegrationTests;

public sealed class HealthAndSecurityTests(SchedulingApiFactory factory) : IClassFixture<SchedulingApiFactory>
{
    [Fact]
    public async Task HealthIsPublicAndHealthy()
    {
        using var client = factory.CreateClient(scopes: []);

        using var response = await client.GetAsync("health", ApiClient.Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(ApiClient.Token));
    }

    [Fact]
    public async Task EveryResponseCarriesTheSecurityHeaders()
    {
        using var client = factory.CreateClient(scopes: []);

        using var response = await client.GetAsync("health", ApiClient.Token);

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Contains("default-src 'none'", response.Headers.GetValues("Content-Security-Policy").Single(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("api/reports/no-shows?from=2026-03-01&to=2026-04-01")]
    [InlineData("api/patients/0b6c0e4e-2a7f-4d0b-8f5e-3c1a9d8e7f60/feed")]
    public async Task AnonymousCallersAreRejected(string path)
    {
        using var client = factory.CreateClient(scopes: []);

        using var response = await client.GetAsync(path, ApiClient.Token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AMissingScopeIsForbidden()
    {
        using var client = factory.CreateClient("appointments.read");

        using var response = await client.BookAsync(Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(3));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
