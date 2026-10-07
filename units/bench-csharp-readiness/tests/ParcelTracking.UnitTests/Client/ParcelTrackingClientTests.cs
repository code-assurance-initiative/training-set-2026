using System.Net;
using Microsoft.Extensions.DependencyInjection;
using ParcelTracking.Client;
using ParcelTracking.UnitTests.TestSupport;

namespace ParcelTracking.UnitTests.Client;

public sealed class ParcelTrackingClientTests
{
    private const string ParcelJson = """
        {"trackingNumber":"NP100200300","carrierCode":"NORDPOST","carrierName":"Nordpost","status":"InTransit",
         "registeredAt":"2026-09-01T08:00:00+00:00","deliveredAt":null,"pickupPointId":null,
         "events":[{"occurredAt":"2026-09-01T10:00:00+00:00","status":"InTransit","location":"Oslo"}]}
        """;

    [Fact]
    public async Task Reads_a_parcel()
    {
        var handler = StubHttpHandler.Returning(HttpStatusCode.OK, ParcelJson);
        var client = new ParcelTrackingClient(new HttpClient(handler) { BaseAddress = new Uri("https://parcels.test/") });

        var parcel = await client.GetParcelAsync("NP100200300", TestContext.Current.CancellationToken);

        Assert.NotNull(parcel);
        Assert.Equal("InTransit", parcel.Status);
        Assert.Equal("Oslo", Assert.Single(parcel.Events).Location);
        Assert.Equal("/api/parcels/NP100200300", handler.Requests[0].Request.RequestUri?.AbsolutePath);
    }

    [Fact]
    public async Task An_unknown_parcel_is_null()
    {
        var client = new ParcelTrackingClient(new HttpClient(StubHttpHandler.Returning(HttpStatusCode.NotFound)) { BaseAddress = new Uri("https://parcels.test/") });
        Assert.Null(await client.GetParcelAsync("NP1", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Registers_a_shipment()
    {
        var handler = StubHttpHandler.Returning(HttpStatusCode.Created, ParcelJson);
        var client = new ParcelTrackingClient(new HttpClient(handler) { BaseAddress = new Uri("https://parcels.test/") });

        var parcel = await client.RegisterShipmentAsync(new ShipmentRegistration("NP100200300", "NORDPOST", "0150"), TestContext.Current.CancellationToken);

        Assert.Equal("NP100200300", parcel.TrackingNumber);
        Assert.Contains("\"destinationPostalCode\":\"0150\"", handler.Requests[0].Body, StringComparison.Ordinal);
    }

    [Fact]
    public void Registration_returns_the_builder_for_the_host_to_configure()
    {
        var services = new ServiceCollection();

        var builder = services.AddParcelTrackingClient(new Uri("https://parcels.test/"));

        Assert.Equal(nameof(ParcelTrackingClient), builder.Name);
        using var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetRequiredService<ParcelTrackingClient>());
    }
}
