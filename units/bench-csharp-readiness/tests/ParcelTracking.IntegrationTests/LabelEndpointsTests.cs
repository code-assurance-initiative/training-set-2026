using System.Net;

namespace ParcelTracking.IntegrationTests;

public sealed class LabelEndpointsTests(TrackingApiFactory factory) : IClassFixture<TrackingApiFactory>
{
    [Fact]
    public async Task The_owner_downloads_the_carrier_label()
    {
        var client = factory.CreateClient("merchant-a", ApiTestData.Read, ApiTestData.Write);
        var trackingNumber = await ApiTestData.RegisterAsync(client, TestContext.Current.CancellationToken);

        var response = await client.GetAsync($"/labels/{trackingNumber}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(FakeCarrierClient.LabelPdf, await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Labels_need_a_token_and_ownership()
    {
        var owner = factory.CreateClient("merchant-a", ApiTestData.Read, ApiTestData.Write);
        var trackingNumber = await ApiTestData.RegisterAsync(owner, TestContext.Current.CancellationToken);

        var anonymous = await factory.CreateClient(merchantId: null).GetAsync($"/labels/{trackingNumber}", TestContext.Current.CancellationToken);
        var stranger = await factory.CreateClient("merchant-b", ApiTestData.Read).GetAsync($"/labels/{trackingNumber}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, stranger.StatusCode);
    }
}
