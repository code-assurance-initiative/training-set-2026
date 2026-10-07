using System.Net;
using System.Net.Http.Json;

namespace ClinicScheduling.IntegrationTests;

public sealed class BookingFlowTests(SchedulingApiFactory factory) : IClassFixture<SchedulingApiFactory>
{
    [Fact]
    public async Task FindBookRescheduleAndCancel()
    {
        using var client = factory.CreateClient(ApiClient.AllScopes);
        await client.PutScheduleAsync();
        var patient = Guid.NewGuid();

        var slots = await (await client.GetAsync(
            $"api/practitioners/{ApiClient.PractitionerId}/slots?from=2026-03-05&to=2026-03-05&durationMinutes=45", ApiClient.Token)).ReadAsync<List<SlotDto>>();
        using var booked = await client.BookAsync(patient, slots[0].Start);
        var appointment = await booked.ReadAsync<AppointmentDto>();
        using var moved = await client.PostAsJsonAsync($"api/appointments/{appointment.Id}/reschedule", new { newStart = slots[4].Start }, ApiClient.Token);
        using var cancelled = await client.PostAsJsonAsync($"api/appointments/{appointment.Id}/cancellation", new { reason = "patient-request" }, ApiClient.Token);
        var fetched = await (await client.GetAsync($"api/appointments/{appointment.Id}", ApiClient.Token)).ReadAsync<AppointmentDto>();

        Assert.Equal(HttpStatusCode.Created, booked.StatusCode);
        Assert.Equal("2026-03-05T08:00:00+01:00", appointment.StartsAt);
        Assert.Equal(HttpStatusCode.OK, moved.StatusCode);
        Assert.Equal(new CancellationDto(0m, false, "free"), await cancelled.ReadAsync<CancellationDto>());
        Assert.Equal("cancelled", fetched.Status);
        Assert.Equal("patient-request", fetched.CancellationReason);
        Assert.Equal(1, fetched.RescheduleCount);
    }

    [Fact]
    public async Task ATakenSlotIsAConflict()
    {
        using var client = factory.CreateClient(ApiClient.AllScopes);
        await client.PutScheduleAsync();
        var start = new DateTimeOffset(2026, 3, 6, 13, 0, 0, TimeSpan.FromHours(1));

        using var first = await client.BookAsync(Guid.NewGuid(), start);
        using var second = await client.BookAsync(Guid.NewGuid(), start);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task InvalidRequestsAreValidationProblems()
    {
        using var client = factory.CreateClient(ApiClient.AllScopes);

        using var response = await client.BookAsync(Guid.Empty, DateTimeOffset.UtcNow, minutes: 5);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("DurationMinutes", await response.Content.ReadAsStringAsync(ApiClient.Token), StringComparison.Ordinal);
    }

    [Fact]
    public async Task BooksATreatmentSeries()
    {
        using var client = factory.CreateClient(ApiClient.AllScopes);
        await client.PutScheduleAsync();

        using var response = await client.PostAsJsonAsync(
            "api/series",
            new { patientId = Guid.NewGuid(), practitionerId = ApiClient.PractitionerId, clinicId = ApiClient.ClinicId, firstDay = "2026-03-30", startTime = "14:00:00", durationMinutes = 30, recurrence = "WEEKLY;COUNT=3;BYDAY=MO,TH" },
            ApiClient.Token);
        var sessions = await response.ReadAsync<List<AppointmentDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(["2026-03-30T14:00:00+02:00", "2026-04-07T14:00:00+02:00", "2026-04-09T14:00:00+02:00"], sessions.Select(s => s.StartsAt));
        Assert.Single(sessions.Select(s => s.SeriesId).Distinct());
    }
}
