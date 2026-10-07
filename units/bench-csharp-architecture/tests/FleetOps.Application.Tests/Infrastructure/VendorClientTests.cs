using System.Net;
using FleetOps.Application.Tests.Support;
using FleetOps.Infrastructure.FuelCards;
using FleetOps.Infrastructure.Geocoding;
using FleetOps.Infrastructure.Parts;
using FleetOps.Infrastructure.Telematics;
using FleetOps.Infrastructure.Tyres;
using Microsoft.Extensions.Options;

namespace FleetOps.Application.Tests.Infrastructure;

public sealed class VendorClientTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task TelematicsReadsOdometerAndPosition()
    {
        using var handler = new StubHttp(HttpStatusCode.OK, """{"vin":"WVWZZZ1KZAW000001","kilometres":15200,"recordedAt":"2026-05-04T09:00:00+00:00","latitude":55.6,"longitude":12.5}""");
        var client = new TelematicsClient(StubHttp.Client(handler));

        var reading = await client.GetOdometerAsync("WVWZZZ1KZAW000001", Ct);
        var position = await client.GetPositionAsync("WVWZZZ1KZAW000001", Ct);

        Assert.Equal(15_200, reading.Kilometres);
        Assert.Equal(55.6, position!.Latitude);
        Assert.True(await client.PingAsync(Ct));
        Assert.Equal("GET /api/v2/vehicles/WVWZZZ1KZAW000001/odometer", handler.Requests[0]);
    }

    [Fact]
    public async Task TelematicsReturnsNoPositionForAnUnknownVehicle()
    {
        using var handler = new StubHttp(HttpStatusCode.NotFound, "{}");
        Assert.Null(await new TelematicsClient(StubHttp.Client(handler)).GetPositionAsync("X", Ct));
        Assert.False(await new TelematicsClient(StubHttp.Client(handler)).PingAsync(Ct));
    }

    [Fact]
    public async Task FuelCardClientPagesTransactionsAndRefusesAnEmptyBody()
    {
        using var page = new StubHttp(HttpStatusCode.OK, """{"items":[],"nextCursor":null}""");
        Assert.Empty((await new FuelCardClient(StubHttp.Client(page)).GetTransactionsAsync("c 1", Ct)).Items);
        Assert.Equal("GET /api/transactions?cursor=c%201", page.Requests[0]);

        using var empty = new StubHttp(HttpStatusCode.OK, "null");
        await Assert.ThrowsAsync<FuelCardApiException>(() => new FuelCardClient(StubHttp.Client(empty)).GetAccountAsync(Ct));
    }

    [Fact]
    public async Task TyreVendorQuotesAndOrders()
    {
        using var quotes = new StubHttp(HttpStatusCode.OK, """[{"supplier":"North","size":{"widthMm":205,"aspectRatio":55,"rimInches":16},"season":0,"unitPrice":81.5,"validUntil":"2026-06-01"}]""");
        var quote = Assert.Single(await new TyreVendorClient(StubHttp.Client(quotes)).QuoteAsync(new TyreQuoteRequest(new TyreSize(205, 55, 16), TyreSeason.Summer, 4), Ct));
        Assert.Equal(81.5m, quote.UnitPrice);

        using var order = new StubHttp(HttpStatusCode.OK, """{"orderNumber":"T-1","vin":"V","quantity":4,"status":1}""");
        var placed = await new TyreVendorClient(StubHttp.Client(order)).OrderAsync(new TyreOrderRequest("V", quote, 4), Ct);
        Assert.Equal(TyreOrderStatus.Confirmed, placed.Status);
    }

    [Fact]
    public async Task PartsSupplierReturnsCatalogueQuotesAndOrders()
    {
        using var catalogue = new StubHttp(HttpStatusCode.OK, """[{"number":{"value":"BP-1"},"description":"Brake pads","category":1}]""");
        var supplier = new PartsSupplierClient(StubHttp.Client(catalogue));
        Assert.Single(await supplier.GetCatalogueAsync(Ct));

        using var none = new StubHttp(HttpStatusCode.OK, "null");
        await Assert.ThrowsAsync<PartsSupplierException>(() => new PartsSupplierClient(StubHttp.Client(none)).QuoteAsync(new PartNumber("BP-1"), Ct));
        await Assert.ThrowsAsync<PartsSupplierException>(() => new PartsSupplierClient(StubHttp.Client(none)).OrderAsync([new PartOrderLine(new PartNumber("BP-1"), 2)], Ct));
    }

    [Fact]
    public async Task GeocoderKeepsConfidentAnswersAndServesRepeatsFromTheCache()
    {
        using var handler = new StubHttp(HttpStatusCode.OK, """{"candidates":[{"lat":55.67,"lon":12.56,"score":0.9},{"lat":1,"lon":1,"score":0.2}]}""");
        var geocoder = new HttpGeocoder(StubHttp.Client(handler), new GeocodingCache(), Options.Create(new GeocodingOptions()));
        var depot = new Address("Havnegade 1", "1058", "København", "DK");

        var first = await geocoder.GeocodeAsync(depot, Ct);
        var second = await geocoder.GeocodeAsync(depot, Ct);

        Assert.Equal(new GeoPoint(55.67, 12.56), first.Point);
        Assert.Equal(first.Point, second.Point);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task GeocoderReturnsNoPointBelowTheConfidenceFloor()
    {
        using var handler = new StubHttp(HttpStatusCode.OK, """{"candidates":[{"lat":1,"lon":1,"score":0.3}]}""");
        var geocoder = new HttpGeocoder(StubHttp.Client(handler), new GeocodingCache(), Options.Create(new GeocodingOptions()));
        var result = await geocoder.GeocodeAsync(new Address("Unknown 9", "0000", "Nowhere", "DK"), Ct);
        Assert.Null(result.Point);
        Assert.Equal(0.3, result.Confidence);
    }
}
