using System.Net;
using FluentAssertions;

namespace Invoicing.Api.IntegrationTests;

public sealed class HealthAndHeadersTests(InvoicingApiFactory factory) : IClassFixture<InvoicingApiFactory>
{
    [Fact]
    public async Task HealthIsAnonymous()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResponsesCarryTheSecurityHeaders()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        response.Headers.GetValues("X-Content-Type-Options").Should().Equal("nosniff");
        response.Headers.GetValues("X-Frame-Options").Should().Equal("DENY");
        response.Headers.GetValues("Content-Security-Policy").Single().Should().Contain("default-src 'none'");
        response.Headers.GetValues("Strict-Transport-Security").Single().Should().Contain("max-age=31536000");
    }
}
