using System.Net;
using System.Net.Http.Json;
using Fx.Conversion.Api.Contracts;
using Fx.Conversion.Api.Security;

namespace Fx.Conversion.IntegrationTests;

public sealed class RatesEndpointTests(ConversionApiFactory factory) : IClassFixture<ConversionApiFactory>
{
    [Fact]
    public async Task ReturnsTheLatestTable()
    {
        using var client = factory.CreateClient(AuthorizationPolicies.RatesRead);

        var rates = await client.GetFromJsonAsync<RatesResponse>("/api/rates", TestContext.Current.CancellationToken);

        Assert.NotNull(rates);
        Assert.Equal("EUR", rates.Base);
        Assert.Equal(new DateOnly(2026, 3, 2), rates.PublishedOn);
        Assert.Equal(1.10m, rates.Rates["USD"]);
    }

    [Fact]
    public async Task RequiresTheRatesScope()
    {
        using var client = factory.CreateClient(AuthorizationPolicies.Quote);

        using var response = await client.GetAsync(new Uri("/api/rates", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RequiresAToken()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/api/rates", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
