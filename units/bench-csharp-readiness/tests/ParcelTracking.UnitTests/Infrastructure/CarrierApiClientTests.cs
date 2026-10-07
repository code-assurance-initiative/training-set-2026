using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using ParcelTracking.Infrastructure.Carriers;
using ParcelTracking.UnitTests.TestSupport;

namespace ParcelTracking.UnitTests.Infrastructure;

public sealed class CarrierApiClientTests
{
    private static CarrierApiClient Client(StubHttpHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://carrier.test/") }, NullLogger<CarrierApiClient>.Instance);

    [Fact]
    public async Task Scans_are_read_from_the_carrier_path()
    {
        var handler = StubHttpHandler.Returning(HttpStatusCode.OK, """
            {"scans":[{"code":"HUB","timestamp":"2026-09-01T10:00:00+00:00","location":"Oslo"}]}
            """);

        var scans = await Client(handler).GetScansAsync("NORDPOST", "NP 1/2", new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.Zero), TestContext.Current.CancellationToken);

        var scan = Assert.Single(scans);
        Assert.Equal("HUB", scan.StatusCode);
        Assert.Equal("Oslo", scan.Location);
        var uri = Assert.Single(handler.Requests).Request.RequestUri;
        Assert.NotNull(uri);
        Assert.Equal("/v2/carriers/NORDPOST/parcels/NP%201%2F2/scans", uri.AbsolutePath);
    }

    [Fact]
    public async Task A_parcel_unknown_to_the_carrier_has_no_scans()
    {
        var scans = await Client(StubHttpHandler.Returning(HttpStatusCode.NotFound)).GetScansAsync("NORDPOST", "NP1", DateTimeOffset.UnixEpoch, TestContext.Current.CancellationToken);
        Assert.Empty(scans);
    }

    [Fact]
    public async Task Server_errors_surface_as_HttpRequestException() =>
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            Client(StubHttpHandler.Returning(HttpStatusCode.BadGateway)).GetScansAsync("NORDPOST", "NP1", DateTimeOffset.UnixEpoch, TestContext.Current.CancellationToken));

    [Fact]
    public async Task A_failed_label_request_returns_null() =>
        Assert.Null(await Client(StubHttpHandler.Returning(HttpStatusCode.ServiceUnavailable)).GetLabelAsync("NORDPOST", "NP1", TestContext.Current.CancellationToken));
}
