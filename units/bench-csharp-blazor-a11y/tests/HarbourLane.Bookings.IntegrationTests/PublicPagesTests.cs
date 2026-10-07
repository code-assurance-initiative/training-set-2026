using System.Net;

namespace HarbourLane.Bookings.IntegrationTests;

public sealed class PublicPagesTests(PortalFactory factory) : IClassFixture<PortalFactory>
{
    [Theory]
    [InlineData("/", "Rooms for every kind of get-together")]
    [InlineData("/rooms", "Rooms")]
    [InlineData("/rooms/main-hall", "Main hall")]
    [InlineData("/terms", "Conditions of hire")]
    public async Task Public_pages_render_on_the_server(string path, string heading)
    {
        using var client = factory.CreatePortalClient();

        var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains($">{heading}</h1>", html, StringComparison.Ordinal);
        Assert.Contains("<html lang=\"en\">", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_health_endpoint_is_anonymous()
    {
        using var client = factory.CreatePortalClient();

        var response = await client.GetAsync(new Uri("/health", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Responses_carry_the_security_headers()
    {
        using var client = factory.CreatePortalClient();

        var response = await client.GetAsync(new Uri("/", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.StartsWith("default-src 'self'", response.Headers.GetValues("Content-Security-Policy").Single(), StringComparison.Ordinal);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
    }
}
