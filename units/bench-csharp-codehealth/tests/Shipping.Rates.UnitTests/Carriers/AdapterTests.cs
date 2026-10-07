using System.Net;
using Shipping.Rates.Core.Carriers;
using Shipping.Rates.Core.Domain;
using Shipping.Rates.UnitTests.TestSupport;

namespace Shipping.Rates.UnitTests.Carriers;

public sealed class AdapterTests
{
    private static QuoteRequest Request => new(TestData.Berlin, TestData.Paris, [TestData.Small], ServiceLevel.Standard, new DateOnly(2026, 3, 10), 250m);

    [Fact]
    public async Task Alder_sends_rounded_weights_and_the_insured_value()
    {
        var handler = new StubHandler(HttpStatusCode.OK, """{"rates":[]}""");
        var adapter = new AlderParcelAdapter(handler.Client(), Loggers.For<AlderParcelAdapter>());

        await adapter.GetRatesAsync(Request, TestContext.Current.CancellationToken);

        var (method, path, body) = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, method);
        Assert.Equal("/v2/rates", path);
        Assert.Contains("\"weightKg\":1.2", body, StringComparison.Ordinal);
        Assert.Contains("\"insuredValue\":250", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Alder_reports_the_error_code_of_a_rejected_request()
    {
        var handler = new StubHandler(HttpStatusCode.BadRequest, """{"errorCode":"POSTCODE_INVALID","message":"bad"}""");
        var adapter = new AlderParcelAdapter(handler.Client(), Loggers.For<AlderParcelAdapter>());

        var error = await Assert.ThrowsAsync<CarrierException>(() => adapter.GetRatesAsync(Request, TestContext.Current.CancellationToken));

        Assert.Equal("POSTCODE_INVALID", error.ErrorCode);
    }

    [Fact]
    public async Task Alder_creates_a_label()
    {
        var handler = new StubHandler(HttpStatusCode.OK, """{"trackingNumber":"A123456789012","labelZpl":"^XA^XZ"}""");
        var adapter = new AlderParcelAdapter(handler.Client(), Loggers.For<AlderParcelAdapter>());

        var label = await adapter.CreateLabelAsync(Request, ServiceLevel.Express, TestContext.Current.CancellationToken);

        Assert.Equal(new CarrierLabel("ALDER", "A123456789012", "^XA^XZ"), label);
        Assert.Contains("ALD-EXP-INT", handler.Requests[0].Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Alder_does_not_collect()
    {
        var adapter = new AlderParcelAdapter(new StubHandler(HttpStatusCode.OK, "{}").Client(), Loggers.For<AlderParcelAdapter>());

        await Assert.ThrowsAsync<NotSupportedException>(() => adapter.SchedulePickupAsync(TestData.Berlin, new DateOnly(2026, 3, 11), TestContext.Current.CancellationToken));
        Assert.False(adapter.Capabilities.SupportsPickup);
    }

    [Theory]
    [InlineData("created", TrackingStatus.AwaitingCollection)]
    [InlineData("delivered", TrackingStatus.Delivered)]
    [InlineData("lost", TrackingStatus.Unknown)]
    public async Task Alder_maps_tracking_states(string state, TrackingStatus expected)
    {
        var adapter = new AlderParcelAdapter(new StubHandler(HttpStatusCode.OK, $$"""{"state":"{{state}}"}""").Client(), Loggers.For<AlderParcelAdapter>());

        Assert.Equal(expected, await adapter.TrackAsync("A123", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Corvid_maps_rates_and_transit_days()
    {
        var handler = new StubHandler(HttpStatusCode.OK, """{"rates":[{"service":"EXP","amount":12.5,"currency":"EUR","transit":"1"},{"service":"XXL","amount":1,"currency":"EUR","transit":"9"}]}""");
        var adapter = new CorvidCourierAdapter(handler.Client(), Loggers.For<CorvidCourierAdapter>());

        var rates = await adapter.GetRatesAsync(Request, TestContext.Current.CancellationToken);

        Assert.Equal(new CarrierRate("CORVID", ServiceLevel.Express, 12.5m, "EUR", 1), Assert.Single(rates));
        Assert.Equal("/v1/rates", handler.Requests[0].Path);
    }

    [Fact]
    public async Task Corvid_reports_a_failed_rate_request()
    {
        var adapter = new CorvidCourierAdapter(new StubHandler(HttpStatusCode.ServiceUnavailable, "").Client(), Loggers.For<CorvidCourierAdapter>());

        var error = await Assert.ThrowsAsync<CarrierException>(() => adapter.GetRatesAsync(Request, TestContext.Current.CancellationToken));

        Assert.Equal("503", error.ErrorCode);
    }

    [Fact]
    public async Task Corvid_creates_labels_tracks_and_schedules_pickups()
    {
        var handler = new StubHandler(HttpStatusCode.OK, """{"awb":"JJD0001","zpl":"^XA^XZ","status":"TODO"}""");
        var adapter = new CorvidCourierAdapter(handler.Client(), Loggers.For<CorvidCourierAdapter>());
        var ct = TestContext.Current.CancellationToken;

        var label = await adapter.CreateLabelAsync(Request, ServiceLevel.Standard, ct);
        var status = await adapter.TrackAsync("JJD0001", ct);
        await adapter.SchedulePickupAsync(TestData.Berlin, new DateOnly(2026, 3, 11), ct);

        Assert.Equal("JJD0001", label.TrackingNumber);
        Assert.Equal(TrackingStatus.AwaitingCollection, status);
        Assert.Equal(["/v1/labels", "/v1/track/JJD0001", "/v1/pickups"], handler.Requests.Select(r => r.Path));
    }
}
