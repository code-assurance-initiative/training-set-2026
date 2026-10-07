using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ClinicScheduling.IntegrationTests;

public sealed class ReportsAndPortalTests(SchedulingApiFactory factory) : IClassFixture<SchedulingApiFactory>
{
    [Fact]
    public async Task ReportsNoShowsPerPractitioner()
    {
        using var client = factory.CreateClient(ApiClient.AllScopes);
        await client.PutScheduleAsync();
        using var booked = await client.BookAsync(Guid.NewGuid(), new DateTimeOffset(2026, 3, 10, 9, 0, 0, TimeSpan.FromHours(1)));
        var appointment = await booked.ReadAsync<AppointmentDto>();
        factory.Clock.SetUtcNow(new DateTimeOffset(2026, 3, 10, 9, 30, 0, TimeSpan.Zero));

        using var noShow = await client.PostAsync($"api/appointments/{appointment.Id}/no-show", null, ApiClient.Token);
        var rows = await (await client.GetAsync("api/reports/no-shows?from=2026-03-01&to=2026-04-01", ApiClient.Token)).ReadAsync<List<NoShowRowDto>>();

        Assert.Equal(HttpStatusCode.OK, noShow.StatusCode);
        Assert.Equal(400m, (await noShow.ReadAsync<CancellationDto>()).Fee);
        Assert.Contains(rows, r => r.PractitionerId == ApiClient.PractitionerId && r.NoShows == 1);
    }

    [Fact]
    public async Task ServesThePortalFeedAndCalendar()
    {
        using var client = factory.CreateClient(ApiClient.AllScopes);
        await client.PutScheduleAsync();
        var patient = Guid.NewGuid();
        using var booked = await client.BookAsync(patient, new DateTimeOffset(2026, 3, 12, 10, 0, 0, TimeSpan.FromHours(1)));
        Assert.Equal(HttpStatusCode.Created, booked.StatusCode);

        using var feed = JsonDocument.Parse(await client.GetStringAsync($"api/patients/{patient}/feed?timeZone=Europe/Copenhagen", ApiClient.Token));
        var ics = await client.GetStringAsync($"api/patients/{patient}/calendar.ics", ApiClient.Token);

        Assert.Equal("2026-03-12T10:00:00+01:00", feed.RootElement.GetProperty("appointments")[0].GetProperty("startsAt").GetString());
        Assert.Contains("DTSTART:20260312T090000Z", ics, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TheReportRejectsAnEmptyRange()
    {
        using var client = factory.CreateClient(ApiClient.AllScopes);

        using var response = await client.GetAsync("api/reports/no-shows?from=2026-03-01&to=2026-03-01", ApiClient.Token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
