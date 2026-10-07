using System.Net;

namespace Warehouse.Stock.IntegrationTests;

public sealed class HealthEndpointTests(StockApiFactory factory) : IClassFixture<StockApiFactory>
{
    [Fact]
    public async Task HealthIsPublicAndHealthy()
    {
        using var client = factory.CreateClient(scopes: []);

        using var response = await client.GetAsync("health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }
}
