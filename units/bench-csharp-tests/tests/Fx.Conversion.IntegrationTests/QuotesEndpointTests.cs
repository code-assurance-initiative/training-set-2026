using System.Net;
using System.Net.Http.Json;
using Fx.Conversion.Api.Contracts;
using Fx.Conversion.Api.Security;
using Fx.Conversion.Quotes;

namespace Fx.Conversion.IntegrationTests;

public sealed class QuotesEndpointTests(ConversionApiFactory factory) : IClassFixture<ConversionApiFactory>
{
    [Fact]
    public async Task CreatesAQuoteThatCanBeReadBackUntilItExpires()
    {
        using var client = factory.CreateClient(AuthorizationPolicies.Quote);

        using var created = await client.PostAsJsonAsync("/api/quotes", new { amount = 1000m, from = "EUR", to = "USD" }, TestContext.Current.CancellationToken);
        var quote = await created.Content.ReadFromJsonAsync<QuoteResponse>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.NotNull(quote);
        Assert.Equal(new MoneyDto(1094.50m, "USD"), quote.Credit);
        Assert.Equal(new MoneyDto(5.00m, "EUR"), quote.Fee);

        var location = created.Headers.Location;
        Assert.NotNull(location);
        Assert.Equal(quote, await client.GetFromJsonAsync<QuoteResponse>(location, TestContext.Current.CancellationToken));

        factory.Clock.Advance(QuoteService.Validity);
        using var expired = await client.GetAsync(location, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, expired.StatusCode);
    }

    [Fact]
    public async Task RejectsAnAmountThatDoesNotCoverTheFee()
    {
        using var client = factory.CreateClient(AuthorizationPolicies.Quote);

        using var response = await client.PostAsJsonAsync("/api/quotes", new { amount = 0.50m, from = "EUR", to = "USD" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
