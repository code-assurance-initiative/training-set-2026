using System.Net;
using System.Net.Http.Json;
using ParcelTracking.Api.Contracts;
using ParcelTracking.Core.Carriers;

namespace ParcelTracking.IntegrationTests;

public sealed class ParcelsEndpointsTests(TrackingApiFactory factory) : IClassFixture<TrackingApiFactory>
{
    [Fact]
    public async Task A_merchant_reads_its_own_parcel_with_the_carrier_name()
    {
        var client = factory.CreateClient("merchant-a", ApiTestData.Read, ApiTestData.Write);
        var trackingNumber = await ApiTestData.RegisterAsync(client, TestContext.Current.CancellationToken);

        var parcel = await client.GetFromJsonAsync<ParcelResponse>($"/api/parcels/{trackingNumber}", TestContext.Current.CancellationToken);

        Assert.Equal(trackingNumber, parcel?.TrackingNumber);
        Assert.Equal("Nordpost", parcel?.CarrierName);
    }

    [Fact]
    public async Task Another_merchants_parcel_is_not_found()
    {
        var owner = factory.CreateClient("merchant-a", ApiTestData.Read, ApiTestData.Write);
        var trackingNumber = await ApiTestData.RegisterAsync(owner, TestContext.Current.CancellationToken);
        var stranger = factory.CreateClient("merchant-b", ApiTestData.Read);

        var response = await stranger.GetAsync($"/api/parcels/{trackingNumber}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Refreshing_applies_the_carriers_scans()
    {
        var client = factory.CreateClient("merchant-a", ApiTestData.Read, ApiTestData.Write);
        var trackingNumber = await ApiTestData.RegisterAsync(client, TestContext.Current.CancellationToken);
        var now = DateTimeOffset.UtcNow;
        factory.Carrier.Scans[trackingNumber] =
        [
            new CarrierScan("ACC", now.AddMinutes(1), "Bergen"),
            new CarrierScan("OFD", now.AddMinutes(2), "Oslo"),
        ];

        var response = await client.PostAsync($"/api/parcels/{trackingNumber}/refresh", content: null, TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        var parcel = await response.Content.ReadFromJsonAsync<ParcelResponse>(TestContext.Current.CancellationToken);
        Assert.Equal("OutForDelivery", parcel?.Status);
        Assert.Equal(["Bergen", "Oslo"], parcel?.Events.Select(e => e.Location));
    }

    [Fact]
    public async Task A_parcel_can_be_redirected_to_a_pickup_point()
    {
        var client = factory.CreateClient("merchant-a", ApiTestData.Read, ApiTestData.Write);
        var trackingNumber = await ApiTestData.RegisterAsync(client, TestContext.Current.CancellationToken);

        var response = await client.PostAsJsonAsync(
            $"/api/parcels/{trackingNumber}/redirect",
            new { pickupPointId = "PUP-0042", holdUntil = "2026-12-01" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var parcel = await client.GetFromJsonAsync<ParcelResponse>($"/api/parcels/{trackingNumber}", TestContext.Current.CancellationToken);
        Assert.Equal("PUP-0042", parcel?.PickupPointId);
    }

    [Fact]
    public async Task Reading_needs_a_token()
    {
        var response = await factory.CreateClient(merchantId: null).GetAsync("/api/parcels/NP000000000", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
