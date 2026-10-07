using System.Net;
using System.Net.Http.Json;
using Fx.Conversion.Api.Contracts;
using Fx.Conversion.Api.Security;

namespace Fx.Conversion.IntegrationTests;

public sealed class AllocationsEndpointTests(ConversionApiFactory factory) : IClassFixture<ConversionApiFactory>
{
    private static readonly int[] ThreeEqualShares = [1, 1, 1];
    private static readonly int[] NoShares = [0, 0];

    [Fact]
    public async Task SplitsByWeightWithoutLosingACent()
    {
        using var client = factory.CreateClient(AuthorizationPolicies.Convert);

        using var response = await client.PostAsJsonAsync("/api/allocations", new { amount = 100m, currency = "EUR", weights = ThreeEqualShares }, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<AllocationResponse>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal([33.34m, 33.33m, 33.33m], body?.Parts.Select(part => part.Amount) ?? []);
    }

    [Fact]
    public async Task RejectsWeightsThatSumToZero()
    {
        using var client = factory.CreateClient(AuthorizationPolicies.Convert);

        using var response = await client.PostAsJsonAsync("/api/allocations", new { amount = 1m, currency = "EUR", weights = NoShares }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
