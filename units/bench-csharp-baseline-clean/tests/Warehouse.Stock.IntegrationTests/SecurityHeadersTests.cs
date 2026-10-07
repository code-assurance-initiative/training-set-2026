using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Warehouse.Stock.IntegrationTests;

public sealed class SecurityHeadersTests(StockApiFactory factory) : IClassFixture<StockApiFactory>
{
    [Fact]
    public async Task ResponsesCarryTheSecurityHeaders()
    {
        using var client = factory.CreateClient(scopes: []);

        using var response = await client.GetAsync("health", TestContext.Current.CancellationToken);

        Assert.Equal("default-src 'none'; frame-ancestors 'none'", Header(response, "Content-Security-Policy"));
        Assert.Equal("nosniff", Header(response, "X-Content-Type-Options"));
        Assert.Equal("DENY", Header(response, "X-Frame-Options"));
        Assert.Equal("no-referrer", Header(response, "Referrer-Policy"));
        Assert.StartsWith("max-age=31536000", Header(response, "Strict-Transport-Security"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task PlainHttpIsRedirectedToHttps()
    {
        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { BaseAddress = new Uri("http://stock.test"), AllowAutoRedirect = false });

        using var response = await client.GetAsync("health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.TemporaryRedirect, response.StatusCode);
        Assert.Equal(Uri.UriSchemeHttps, response.Headers.Location?.Scheme);
    }

    private static string Header(HttpResponseMessage response, string name) =>
        string.Join(",", response.Headers.GetValues(name));
}
