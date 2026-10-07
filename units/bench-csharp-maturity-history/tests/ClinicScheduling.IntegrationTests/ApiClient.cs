using System.Net.Http.Json;

namespace ClinicScheduling.IntegrationTests;

public static class ApiClient
{
    public const string AllScopes = "appointments.read appointments.write schedules.write reports.read portal.read";

    public static readonly Guid PractitionerId = Guid.Parse("2a8e4f1c-7b3d-4e9a-a6c5-1d0f8b2e3c47");
    public static readonly Guid ClinicId = Guid.Parse("9c3b5d7e-1f2a-4b6c-8d0e-2f4a6b8c0d1e");

    private static readonly string[] Weekdays = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"];
    private static readonly string[] Skills = ["sports"];

    public static CancellationToken Token => TestContext.Current.CancellationToken;

    /// <summary>Gives the practitioner weekday hours 08:00-16:00 in Copenhagen.</summary>
    public static async Task PutScheduleAsync(this HttpClient client)
    {
        var hours = Weekdays.Select(day => new { day, opens = "08:00:00", closes = "16:00:00", breakStarts = "12:00:00", breakEnds = "12:30:00", inClinicOnly = false });
        var body = new { countryCode = "DK", timeZone = "Europe/Copenhagen", hours, skills = Skills, offersTelehealth = true, maxAppointmentsPerDay = 10 };
        using var response = await client.PutAsJsonAsync($"api/practitioners/{PractitionerId}/schedule", body, Token);
        Assert.True(response.IsSuccessStatusCode, $"PUT schedule returned {(int)response.StatusCode}");
    }

    public static async Task<HttpResponseMessage> BookAsync(this HttpClient client, Guid patientId, DateTimeOffset start, int minutes = 45) =>
        await client.PostAsJsonAsync(
            "api/appointments",
            new { patientId, practitionerId = PractitionerId, clinicId = ClinicId, start, durationMinutes = minutes, telehealth = false },
            Token);

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<T>(Token);
        Assert.NotNull(body);
        return body;
    }
}
