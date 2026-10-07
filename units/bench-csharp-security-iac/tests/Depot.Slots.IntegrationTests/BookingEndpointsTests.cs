using System.Net;
using System.Net.Http.Json;

namespace Depot.Slots.IntegrationTests;

public sealed class BookingEndpointsTests(SlotsApiFactory factory) : IClassFixture<SlotsApiFactory>
{
    private sealed record Booked(Guid Id, string DockCode, string CarrierReference, DateTimeOffset StartsAt, DateTimeOffset EndsAt);

    private static object Request(string dock, string carrier, int hour, int duration = 30) => new
    {
        dockCode = dock,
        carrierReference = carrier,
        startsAt = new DateTimeOffset(2026, 3, 3, hour, 0, 0, TimeSpan.Zero),
        durationMinutes = duration,
    };

    [Fact]
    public async Task ABookingCanBeCreatedListedAndCancelled()
    {
        var ct = TestContext.Current.CancellationToken;
        var writer = factory.CreateClient("slots.read", "slots.write");

        var created = await writer.PostAsJsonAsync("/api/bookings", Request("D04", "CARR-4100", 10), ct);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var booking = await created.Content.ReadFromJsonAsync<Booked>(ct);
        Assert.NotNull(booking);

        var day = await writer.GetFromJsonAsync<Booked[]>("/api/docks/D04/bookings?date=2026-03-03", ct);
        Assert.Equal(["CARR-4100"], day?.Select(b => b.CarrierReference));

        Assert.Equal(HttpStatusCode.NoContent, (await writer.DeleteAsync($"/api/bookings/{booking.Id}", ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await writer.DeleteAsync($"/api/bookings/{booking.Id}", ct)).StatusCode);
    }

    [Fact]
    public async Task AnOverlappingBookingIsAConflict()
    {
        var ct = TestContext.Current.CancellationToken;
        var writer = factory.CreateClient("slots.write");

        await writer.PostAsJsonAsync("/api/bookings", Request("D05", "CARR-5100", 11, 60), ct);
        var clash = await writer.PostAsJsonAsync("/api/bookings", Request("D05", "CARR-5200", 11), ct);

        Assert.Equal(HttpStatusCode.Conflict, clash.StatusCode);
    }

    [Fact]
    public async Task AMalformedRequestIsAValidationProblem()
    {
        var ct = TestContext.Current.CancellationToken;
        var writer = factory.CreateClient("slots.write");

        var response = await writer.PostAsJsonAsync("/api/bookings", Request("dock five", "carr 1", 11), ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(ct);
        Assert.Contains("DockCode", body, StringComparison.Ordinal);
        Assert.Contains("CarrierReference", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnUnknownDockIsUnprocessable()
    {
        var ct = TestContext.Current.CancellationToken;
        var writer = factory.CreateClient("slots.write");

        var response = await writer.PostAsJsonAsync("/api/bookings", Request("D99", "CARR-9900", 11), ct);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
}
