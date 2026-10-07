using System.Text.Json;
using ClinicScheduling.Domain.Appointments;
using ClinicScheduling.Domain.Availability;
using ClinicScheduling.Infrastructure.PatientPortal;
using static ClinicScheduling.UnitTests.TestData;

namespace ClinicScheduling.UnitTests.PatientPortal;

public sealed class PortalAppointmentFeedTests
{
    private static readonly Guid PatientId = Guid.Parse("0b6c0e4e-2a7f-4d0b-8f5e-3c1a9d8e7f60");

    [Fact]
    public void WritesEveryAppointmentNewestFirstInTheGivenZone()
    {
        var early = Appointment.Book(PatientId, PractitionerId, Guid.NewGuid(), new TimeRange(At(Monday, 9), At(Monday, 9, 45)), false);
        var late = Appointment.Book(PatientId, PractitionerId, Guid.NewGuid(), new TimeRange(At(Monday.AddDays(7), 9), At(Monday.AddDays(7), 10)), true);
        late.Cancel(CancellationReason.PatientIllness, At(Monday.AddDays(6), 18));
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen");

        using var feed = JsonDocument.Parse(PortalAppointmentFeed.Serialize(PatientId, [early, late], zone));

        var items = feed.RootElement.GetProperty("appointments").EnumerateArray().ToList();
        Assert.Equal(PatientId, feed.RootElement.GetProperty("patientId").GetGuid());
        Assert.Equal(late.Id, items[0].GetProperty("id").GetGuid());
        Assert.Equal("2026-03-09T10:00:00+01:00", items[0].GetProperty("startsAt").GetString());
        Assert.Equal(60, items[0].GetProperty("durationMinutes").GetInt32());
        Assert.Equal("cancelled", items[0].GetProperty("status").GetString());
        Assert.Equal("patient-illness", items[0].GetProperty("cancellationReason").GetString());
        Assert.True(items[0].GetProperty("isTelehealth").GetBoolean());
        Assert.Equal("booked", items[1].GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, items[1].GetProperty("cancelledAt").ValueKind);
        Assert.Equal(0, items[1].GetProperty("rescheduleCount").GetInt32());
    }

    [Fact]
    public void WritesAnEmptyFeed()
    {
        Assert.Equal(
            $"{{\"patientId\":\"{PatientId}\",\"appointments\":[]}}",
            PortalAppointmentFeed.Serialize(PatientId, [], TimeZoneInfo.Utc));
    }
}
