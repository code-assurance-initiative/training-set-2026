using System.Net;
using System.Net.Http.Json;
using Fx.Conversion.Api.Contracts;
using Fx.Conversion.Api.Security;

namespace Fx.Conversion.IntegrationTests;

public sealed class ConversionsEndpointTests(ConversionApiFactory factory) : IClassFixture<ConversionApiFactory>
{
    [Fact]
    public async Task ConvertsAndReportsTheRateUsed()
    {
        using var client = factory.CreateClient(AuthorizationPolicies.Convert);

        using var response = await client.PostAsJsonAsync("/api/conversions", new { amount = 100m, from = "EUR", to = "USD" }, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<ConversionResponse>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(new MoneyDto(110.00m, "USD"), body.Result);
        Assert.Equal(1.10m, body.Rate);
    }

    [Fact]
    public async Task HonoursTheRequestedRounding()
    {
        using var client = factory.CreateClient(AuthorizationPolicies.Convert);

        using var response = await client.PostAsJsonAsync("/api/conversions", new { amount = 10.03m, from = "EUR", to = "JPY", rounding = 2 }, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<ConversionResponse>(TestContext.Current.CancellationToken);

        Assert.Equal(new MoneyDto(1604m, "JPY"), body?.Result);
    }

    [Theory]
    [InlineData("EUR", "XAU")]
    [InlineData("eur", "USD")]
    public async Task RejectsUnknownOrMalformedCurrencies(string from, string to)
    {
        using var client = factory.CreateClient(AuthorizationPolicies.Convert);

        using var response = await client.PostAsJsonAsync("/api/conversions", new { amount = 1m, from, to }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
