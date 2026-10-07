using System.Net;
using System.Net.Http.Headers;
using Warehouse.Stock.Api.Security;

namespace Warehouse.Stock.IntegrationTests;

public sealed class AuthorizationTests(StockApiFactory factory) : IClassFixture<StockApiFactory>
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("api/skus")]
    [InlineData("api/bins")]
    [InlineData("api/stock/BOLT-M8-40")]
    [InlineData("api/reservations/0b0f8a8e-5d5c-4d8e-9a43-3f4f2a6f4c11")]
    public async Task EveryApiEndpointRequiresAToken(string path)
    {
        using var client = factory.CreateClient(scopes: []);

        using var response = await client.GetAsync(path, Token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Bearer", Assert.Single(response.Headers.WwwAuthenticate).Scheme);
    }

    [Fact]
    public async Task ATokenWithoutTheRequiredScopeIsForbidden()
    {
        using var client = factory.CreateClient(AuthorizationPolicies.StockRead);

        var status = await client.StatusOfPostAsync("api/skus", new { code = "NUT-M8", description = "Hex nut", unitOfMeasure = "EA" });

        Assert.Equal(HttpStatusCode.Forbidden, status);
    }

    [Fact]
    public async Task ATokenForAnotherAudienceIsRejected()
    {
        using var client = factory.CreateClient(scopes: []);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", factory.Tokens.Create("another-api", AuthorizationPolicies.StockRead));

        using var response = await client.GetAsync("api/skus", Token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ATokenWithTheReadScopeCanList()
    {
        using var client = factory.CreateClient(AuthorizationPolicies.StockRead);

        using var response = await client.GetAsync("api/bins", Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
