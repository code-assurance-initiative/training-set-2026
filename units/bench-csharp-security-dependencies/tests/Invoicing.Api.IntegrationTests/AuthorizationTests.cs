using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Invoicing.Api.IntegrationTests;

public sealed class AuthorizationTests(InvoicingApiFactory factory) : IClassFixture<InvoicingApiFactory>
{
    [Theory]
    [InlineData("/invoices/pdf")]
    [InlineData("/invoices/ubl")]
    [InlineData("/erp/invoices")]
    public async Task AnonymousCallersAreRejected(string path)
    {
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(path, RenderRequests.Valid(), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TheErpScopeDoesNotGrantRendering()
    {
        using var client = factory.CreateClient("erp.submit");

        using var response = await client.PostAsJsonAsync("/invoices/pdf", RenderRequests.Valid(), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ATokenForAnotherAudienceIsRejected()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", factory.Tokens.Create("some-other-api", "invoices.render"));

        using var response = await client.PostAsJsonAsync("/invoices/pdf", RenderRequests.Valid(), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
