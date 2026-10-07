using System.Net;
using System.Net.Http.Json;
using ParcelTracking.Api.Contracts;

namespace ParcelTracking.IntegrationTests;

public sealed class ShipmentsEndpointsTests(TrackingApiFactory factory) : IClassFixture<TrackingApiFactory>
{
    [Fact]
    public async Task A_merchant_registers_a_shipment()
    {
        var client = factory.CreateClient("merchant-a", ApiTestData.Read, ApiTestData.Write);
        var trackingNumber = ApiTestData.NewTrackingNumber();

        var response = await client.PostAsJsonAsync(
            "/api/shipments",
            new { trackingNumber, carrierCode = "NORDPOST", destinationPostalCode = "0150" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"/api/parcels/{trackingNumber}", response.Headers.Location?.OriginalString);
        var parcel = await response.Content.ReadFromJsonAsync<ParcelResponse>(TestContext.Current.CancellationToken);
        Assert.Equal("Registered", parcel?.Status);
    }

    [Fact]
    public async Task Registering_the_same_tracking_number_twice_conflicts()
    {
        var client = factory.CreateClient("merchant-a", ApiTestData.Read, ApiTestData.Write);
        var trackingNumber = await ApiTestData.RegisterAsync(client, TestContext.Current.CancellationToken);

        var response = await client.PostAsJsonAsync(
            "/api/shipments",
            new { trackingNumber, carrierCode = "NORDPOST", destinationPostalCode = "0150" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData("short", "NORDPOST", "0150")]
    [InlineData("np100200300", "NORDPOST", "0150")]
    [InlineData("NP100200300", "nordpost", "0150")]
    [InlineData("NP100200300", "NORDPOST", "")]
    public async Task Invalid_registrations_are_rejected(string trackingNumber, string carrierCode, string postalCode)
    {
        var client = factory.CreateClient("merchant-a", ApiTestData.Read, ApiTestData.Write);

        var response = await client.PostAsJsonAsync(
            "/api/shipments",
            new { trackingNumber, carrierCode, destinationPostalCode = postalCode },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Registering_needs_the_write_scope()
    {
        var anonymous = factory.CreateClient(merchantId: null);
        var reader = factory.CreateClient("merchant-a", ApiTestData.Read);
        var body = new { trackingNumber = "NP100200399", carrierCode = "NORDPOST", destinationPostalCode = "0150" };

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/shipments", body, TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await reader.PostAsJsonAsync("/api/shipments", body, TestContext.Current.CancellationToken)).StatusCode);
    }
}
