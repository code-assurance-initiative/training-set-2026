using System.Net;
using System.Net.Http.Json;

namespace Depot.Slots.IntegrationTests;

public sealed class SecurityTests(SlotsApiFactory factory) : IClassFixture<SlotsApiFactory>
{
    [Fact]
    public async Task BookingsRequireAToken()
    {
        var response = await factory.CreateClient().GetAsync("/api/docks/D01/bookings?date=2026-03-03", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AReadOnlyTokenCannotBook()
    {
        var reader = factory.CreateClient("slots.read");

        var response = await reader.PostAsJsonAsync("/api/bookings", new { dockCode = "D01" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ATokenForAnotherAudienceIsRejected()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", factory.Tokens.Create("another-api", "slots.read"));

        var response = await client.GetAsync("/api/docks/D01/bookings?date=2026-03-03", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task HealthEndpointsAreAnonymous(string path)
    {
        var response = await factory.CreateClient().GetAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ResponsesCarryTheSecurityHeaders()
    {
        var response = await factory.CreateClient().GetAsync("/health/ready", TestContext.Current.CancellationToken);

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.StartsWith("default-src 'none'", response.Headers.GetValues("Content-Security-Policy").Single(), StringComparison.Ordinal);
        Assert.True(response.Headers.Contains("Strict-Transport-Security"));
    }
}
