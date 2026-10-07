using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Stock.Api.Contracts;
using Warehouse.Stock.Api.Security;

namespace Warehouse.Stock.IntegrationTests;

public sealed class CatalogEndpointsTests(StockApiFactory factory) : IClassFixture<StockApiFactory>
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private readonly HttpClient _client = factory.CreateClient(AuthorizationPolicies.StockRead, AuthorizationPolicies.StockWrite);

    [Fact]
    public async Task ARegisteredSkuIsCreatedAndCanBeFetched()
    {
        using var created = await _client.PostOkAsync(
            "api/skus", new { code = "WASHER-M8", description = "Flat washer M8", unitOfMeasure = "EA" });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal("/api/skus/WASHER-M8", created.Headers.Location?.AbsolutePath);
        var fetched = await _client.GetFromJsonAsync<SkuResponse>("api/skus/WASHER-M8", Token);
        Assert.Equal(new SkuResponse("WASHER-M8", "Flat washer M8", "EA"), fetched);
    }

    [Theory]
    [InlineData("w", "Lower-case, too short")]
    [InlineData("WASHER M8", "Contains a space")]
    public async Task AMalformedSkuCodeIsAValidationProblem(string code, string because)
    {
        using var response = await _client.PostAsJsonAsync(
            "api/skus", new { code, description = because, unitOfMeasure = "EA" }, Token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.ReadAsync<ValidationProblemDetails>();
        Assert.Contains("Code", problem.Errors.Keys);
    }

    [Fact]
    public async Task ABinWithZeroCapacityIsAValidationProblem()
    {
        var status = await _client.StatusOfPostAsync("api/bins", new { code = "C01-01-01", zone = "COLD", capacity = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, status);
    }

    [Fact]
    public async Task ADuplicateBinIsAConflict()
    {
        await _client.PostOkAsync("api/bins", new { code = "C02-01-01", zone = "COLD", capacity = 10 });

        var status = await _client.StatusOfPostAsync("api/bins", new { code = "C02-01-01", zone = "COLD", capacity = 10 });

        Assert.Equal(HttpStatusCode.Conflict, status);
    }

    [Fact]
    public async Task PageSizeIsBounded()
    {
        using var response = await _client.GetAsync("api/bins?pageSize=500", Token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AnUnknownSkuIsANotFoundProblem()
    {
        using var response = await _client.GetAsync("api/skus/NOPE-404", Token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
